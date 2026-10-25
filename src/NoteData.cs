using System;
using System.Runtime.Serialization;

[DataContract]
public class NoteData
{
    [DataMember]
    public string Id = Guid.NewGuid().ToString("N");
    [DataMember]
    public string Rtf = "";
    [DataMember]
    public int X = 100, Y = 100, W = 320, H = 340;
    [DataMember]
    public int Color = unchecked((int)0xFFFFE7A0);
    [DataMember]
    public double Opacity = 1;
    [DataMember]
    public bool Top, Locked, Folded;
    [DataMember]
    public bool Desktop = true;
}
