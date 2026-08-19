#version 330 core
uniform sampler2D diffuse;
uniform sampler2D lightmap;
in vec2 fragTexCoord;
in vec2 vertexLightmapCoord;
out vec4 fragColor;
void main(){ vec4 d=texture(diffuse,fragTexCoord); vec4 l=texture(lightmap,vertexLightmapCoord); fragColor=vec4(d.rgb*l.rgb,d.a); }
