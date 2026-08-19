using System.Text;

namespace GoQuake2.Q2File;

public static class PakLoader
{
    public static Dictionary<string,PakFile> LoadQ2Pak(Stream s)
    {
        using var br=new BinaryReader(s,Encoding.ASCII,true);
        s.Position=0;
        var magic=Encoding.ASCII.GetString(br.ReadBytes(4));
        if(magic!="PACK") throw new InvalidDataException($"PAK Header: Wrong magic {magic}");
        uint off=br.ReadUInt32(), len=br.ReadUInt32();
        int count=(int)len/64;
        Console.WriteLine($"PAK file contains {count} files");
        var map=new Dictionary<string,PakFile>(StringComparer.OrdinalIgnoreCase);
        s.Position=off;
        for(int i=0; i<count; i++)
        {
            string fn=ReadFixed(br,56);
            uint fo=br.ReadUInt32(), fl=br.ReadUInt32();
            map[fn]=new(fn,fo,fl);
        }
        return map;
    }
    public static MapData LoadQ2BspFromPak(Stream s,Dictionary<string,PakFile> files,string name)
    {
        if(!files.TryGetValue(name,out var f)) throw new FileNotFoundException($"BSP filename {name} doesn't exist in PAK");
        return BspLoader.Load(new SubStream(s,f.Offset,f.Length));
    }
    public static (byte[] Image,WalHeader Header) LoadQ2WalFromPak(Stream s,Dictionary<string,PakFile> files,string name)
    {
        if(!files.TryGetValue(name,out var f)) throw new FileNotFoundException($"Texture filename {name} doesn't exist in PAK");
        return WalLoader.Load(new SubStream(s,f.Offset,f.Length));
    }
    internal static string ReadFixed(BinaryReader br,int n)
    {
        var b=br.ReadBytes(n);
        int z=Array.IndexOf(b,(byte)0);
        return Encoding.ASCII.GetString(b,0,z<0?n:z);
    }
    private sealed class SubStream:Stream
    {
        readonly Stream b;
        readonly long start,len;
        long p;
        public SubStream(Stream b,long start,long len)
        {
            this.b=b;
            this.start=start;
            this.len=len;
        }
        public override bool CanRead=>true;
        public override bool CanSeek=>true;
        public override bool CanWrite=>false;
        public override long Length=>len;
        public override long Position
        {
            get=>p;
            set=>Seek(value,SeekOrigin.Begin);
        }
        public override int Read(byte[] buffer,int offset,int count)
        {
            count=(int)Math.Min(count,len-p);
            if(count<=0)return 0;
            lock(b)
            {
                b.Position=start+p;
                int r=b.Read(buffer,offset,count);
                p+=r;
                return r;
            }
        }
        public override long Seek(long o,SeekOrigin so)
        {
            long np=so switch
            {
                SeekOrigin.Begin=>o,SeekOrigin.Current=>p+o,SeekOrigin.End=>len+o,_=>p
            }
            ;
            if(np<0||np>len)throw new IOException();
            p=np;
            return p;
        }
        public override void Flush()
        {
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer,int offset,int count)
        {
            throw new NotSupportedException();
        }
    }
}
