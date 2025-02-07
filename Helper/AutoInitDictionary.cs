namespace MFAWPF.Helper;

public class AutoInitDictionary : Dictionary<string, int>
{
    public AutoInitDictionary()
    {
        this["exploreCount"] = 0;
    }

    // 重写索引器，确保不存在的键被访问时初始化为0
    public new int this[string key]
    {
        get
        {
            if (!ContainsKey(key))
            {
                this[key] = 0;
            }

            return base[key];
        }
        set => base[key] = value;
    }
}