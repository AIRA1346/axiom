using System;

[Serializable]
public class ItemInstance
{
    public string InstanceId;
    public string ItemId;
    public int Count;
    public int EnhanceLevel;

    public ItemInstance()
    {
        InstanceId = Guid.NewGuid().ToString();
    }
}
