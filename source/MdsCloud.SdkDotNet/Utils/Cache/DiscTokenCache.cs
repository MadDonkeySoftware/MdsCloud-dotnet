using MadDonkeySoftware.SystemWrappers.IO;
using MadDonkeySoftware.SystemWrappers.Runtime;
using Newtonsoft.Json;

namespace MdsCloud.SdkDotNet.Utils.Cache;

public class DiscTokenCache : ITokenCache
{
    private readonly IPath _path;
    private readonly IFile _file;
    private readonly IDirectory _directory;
    private readonly IEnvironment _environment;
    private Dictionary<string, string> _data = new();

    private string _cacheFile = string.Empty;
    private string _settingsDir = string.Empty;

    public DiscTokenCache()
    {
        _path = new PathWrapper();
        _file = new FileWrapper();
        _directory = new DirectoryWrapper();
        _environment = new EnvironmentWrapper();
    }

    private string SettingsDir
    {
        get
        {
            if (_settingsDir == string.Empty)
            {
                _settingsDir = _path.Join(
                    _environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".mds"
                );
            }

            return _settingsDir;
        }
    }

    private string CacheFile
    {
        get
        {
            if (_cacheFile == string.Empty)
            {
                _cacheFile = _path.Join(SettingsDir, "cache");
            }

            return _cacheFile;
        }
    }

    private void WriteCache()
    {
        if (!_directory.Exists(SettingsDir))
        {
            _directory.CreateDirectory(SettingsDir);
        }
        _file.WriteAllText(CacheFile, JsonConvert.SerializeObject(_data));
    }

    public void Set(string key, string value)
    {
        _data[key] = value;
        WriteCache();
    }

    public string? Get(string key)
    {
        if (!_data.ContainsKey(key) && _file.Exists(CacheFile))
        {
            var fileData = _file.ReadAllText(CacheFile);
            var converted = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileData);
            if (converted != null)
            {
                _data = converted;
            }
        }

        return _data.TryGetValue(key, out var cacheValue) ? cacheValue : null;
    }

    public void Remove(string key)
    {
        _data.Remove(key);
        WriteCache();
    }

    public void RemoveAll()
    {
        _data.Clear();
        WriteCache();
    }
}
