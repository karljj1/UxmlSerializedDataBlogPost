using System;
using UnityEngine.UIElements;

[Serializable]
public class Ammo
{
    public int count;
    public int maxCount;
}

[UxmlObject]
public partial class Gun : Item
{
    [UxmlAttribute]
    public float damage;

    [UxmlAttribute]
    public Ammo ammo = new Ammo { count = 10, maxCount = 10 };
}