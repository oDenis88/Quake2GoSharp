using GoQuake2.Q2File;

namespace GoQuake2.Engine;

public sealed class TreeLeaf
{
    public int LeafIndex;
    public int[] Faces=[];
}
public sealed class BspTree
{
    public TreeLeaf[] TreeLeaves;
    public BspTree(MapData m)
    {
        var all=new TreeLeaf[m.BspLeaves.Length];
        var by=new Dictionary<ushort,List<TreeLeaf>>();
        for(int i=0; i<m.BspLeaves.Length; i++)
        {
            var l=m.BspLeaves[i];
            var faces=new int[l.NumLeafFaces];
            for(int j=0; j<faces.Length; j++)faces[j]=m.LeafFaces[l.FirstLeafFace+j];
            var t=new TreeLeaf
            {
                LeafIndex=i,Faces=faces
            }
            ;
            all[i]=t;
            if(!by.TryGetValue(l.Cluster,out var list))by[l.Cluster]=list=[];
            list.Add(t);
        }
        var facesIn=new Dictionary<ushort,int[]>();
        foreach(var kv in by)facesIn[kv.Key]=kv.Value.SelectMany(x=>x.Faces).Distinct().ToArray();
        var visible=new Dictionary<ushort,int[]>();
        foreach(var kv in facesIn)
        {
            ushort cluster=kv.Key;
            if(cluster==Game.ClusterInvalidId||cluster>=m.VisibilityOffsets.Length)continue;
            var set=new HashSet<int>(kv.Value);
            int v=checked((int)m.VisibilityOffsets[cluster].Pvs),other=0,n=m.VisibilityOffsets.Length;
            while(other<n&&v<m.VisibilityData.Length)
            {
                byte b=m.VisibilityData[v];
                if(b==0)
                {
                    v++;
                    if(v>=m.VisibilityData.Length)break;
                    other+=8*m.VisibilityData[v];
                }
                else
                {
                    for(int bit=0; bit<8&&other<n; bit++,other++)if((b&(1<<bit))!=0&&facesIn.TryGetValue((ushort)other,out var fs))set.UnionWith(fs);
                }
                v++;
            }
            visible[cluster]=set.OrderBy(x=>x).ToArray();
        }
        TreeLeaves=new TreeLeaf[all.Length];
        for(int i=0; i<all.Length; i++)
        {
            ushort c=m.BspLeaves[i].Cluster;
            TreeLeaves[i]=new TreeLeaf
            {
                LeafIndex=i,Faces=c!=Game.ClusterInvalidId&&visible.TryGetValue(c,out var fs)?fs:[]
            }
            ;
        }
    }
    public TreeLeaf FindLeafNode(int start,MapData m,float[] pos)
    {
        int id=start;
        while(id>=0)
        {
            var n=m.Nodes[id];
            var p=m.Planes[checked((int)n.Plane)];
            float d=p.Type<3?pos[checked((int)p.Type)]-p.Distance:pos[0]*p.Normal[0]+pos[1]*p.Normal[1]+pos[2]*p.Normal[2]-p.Distance;
            id=d<0?n.BackChild:n.FrontChild;
        }
        return TreeLeaves[-(id+1)];
    }
}
