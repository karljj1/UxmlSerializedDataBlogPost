using UnityEngine.UIElements;

[UxmlObject]
public partial class HealthPack : Item
{
    [UxmlAttribute]
    public float healAmount = 100;

    public HealthPack()
    {
        name = "Health Pack";
    }
}