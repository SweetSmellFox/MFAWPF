using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Extensions;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MFAWPF.Helper.ValueType;

public partial class MFAHotKey : ObservableObject
{
    public static readonly MFAHotKey NOTSET = new()
    {
        ResourceKey = "HotKeyNotSet",
        IsTip = true
    };

    public static readonly MFAHotKey ERROR = new()
    {
        ResourceKey = "HotKeyOccupiedWarning",
        IsTip = true
    };

    public static readonly MFAHotKey PRESSING = new()
    {
        ResourceKey = "HotKeyPressing",
        IsTip = true
    };

    public bool IsTip { get; set; }

    public MFAHotKey()
    {
        IsTip = false;
        LanguageHelper.LanguageChanged += OnLanguageChanged;
    }
    private void OnLanguageChanged(object sender, EventArgs e)
    {
        UpdateName();
    }

    public MFAHotKey(Key key, ModifierKeys modifiers)
    {
        this.Key = key;
        this.Modifiers = modifiers;
        UpdateName();
    }


    public Key Key { get; set; }

    public ModifierKeys Modifiers { get; set; }

    [ObservableProperty] private string _name;
    public string ResourceKey { get; set; } = string.Empty;
    public bool Equals(MFAHotKey? other)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Equals(other.Key, Key) && Equals(other.Modifiers, Modifiers);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        return obj.GetType() == typeof(MFAHotKey) && Equals((MFAHotKey)obj);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode() * 397 ^ Modifiers.GetHashCode();
    }

    public void UpdateName()
    {
        if (IsTip)
        {
            Name = ResourceKey.ToLocalization();
        }
        else
            Name = ToString();
    }

    public override string ToString()
    {
        if (IsTip)
        {
            return ResourceKey;
        }
        else
        {
            var str = new StringBuilder();

            if (Modifiers.HasFlag(ModifierKeys.Control))
            {
                str.Append("Ctrl + ");
            }

            if (Modifiers.HasFlag(ModifierKeys.Shift))
            {
                str.Append("Shift + ");
            }

            if (Modifiers.HasFlag(ModifierKeys.Alt))
            {
                str.Append("Alt + ");
            }

            if (Modifiers.HasFlag(ModifierKeys.Windows))
            {
                str.Append("Win + ");
            }

            str.Append(Key);

            return str.ToString();
        }
    }

    #region 新增的字符串解析方法

    private static readonly Dictionary<string, ModifierKeys> ModifierMap = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "Ctrl", ModifierKeys.Control
        },
        {
            "Shift", ModifierKeys.Shift
        },
        {
            "Alt", ModifierKeys.Alt
        },
        {
            "Win", ModifierKeys.Windows
        }
    };

    public static MFAHotKey Parse(string input)
    {
        if (input == "HotKeyOccupiedWarning")
            return ERROR;
        if (input == "HotKeyPressing")
            return PRESSING;
        if (string.IsNullOrWhiteSpace(input) || input == "HotKeyNotSet")
            return NOTSET;

        // 标准化分隔符
        string standardized = Regex.Replace(input, @"\s*\+\s*", " + ");
        string[] parts = standardized.Split(
        [
            " + "
        ], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return NOTSET;

        ModifierKeys modifiers = ModifierKeys.None;
        Key key;

        // 解析修饰键（除最后一个部分外）
        for (int i = 0; i < parts.Length - 1; i++)
        {
            string part = parts[i].Trim();
            if (!ModifierMap.TryGetValue(part, out ModifierKeys mod))
                return NOTSET;

            modifiers |= mod;
        }

        // 解析主键（最后一个部分）
        string keyPart = parts[^1].Trim();
        if (!Enum.TryParse(keyPart, true, out key))
            return NOTSET;
        var result = new MFAHotKey(key, modifiers);
        return result;
    }

    /// <summary>
    /// 尝试解析字符串为热键
    /// </summary>
    public static bool TryParse(string input, out MFAHotKey result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            result = NOTSET;
            ;
            return false;
        }
    }

    #endregion
}
