using UnityEngine;
using UnityEngine.UIElements;

[UxmlObject]
public partial class Sword : Item
{
    [UxmlAttribute, Range(1, 100)]
    public float slashDamage;
}