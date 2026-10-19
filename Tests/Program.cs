using Flatline;

string testDirectory = Environment.GetEnvironmentVariable("FLATLINE_TEST_TEMP_ROOT")
    ?? throw new InvalidOperationException("Set FLATLINE_TEST_TEMP_ROOT to a dedicated temporary directory.");
string sandbox = Path.Combine(testDirectory, "flatline-path-tests-" + Guid.NewGuid().ToString("N"));
string root = Path.Combine(sandbox, "Saves");
string profile = Path.Combine(root, "TestProfile");
string save = Path.Combine(profile, "SaveGame_1");
Directory.CreateDirectory(save);
Directory.CreateDirectory(Path.Combine(profile, "SaveGame_2"));
Directory.CreateDirectory(Path.Combine(sandbox, "OtherGame", "TestProfile", "SaveGame_1"));
try
{
    Check(true, save, 1, "exact active slot");
    Check(false, save, 2, "different slot number");
    Check(false, profile, 1, "profile parent");
    Check(false, root, 1, "saves root");
    Check(false, Path.Combine(sandbox, "OtherGame", "TestProfile", "SaveGame_1"), 1, "other game");
    Check(false, Path.Combine(profile, "SaveGame_2"), 1, "other save");
    Check(false, Path.Combine(profile, "..", "SaveGame_1"), 1, "path traversal");
    Check(false, "SaveGame_1", 1, "relative path");
    Check(false, Path.Combine(profile, "SaveGame_3"), 3, "nonexistent path");

    try
    {
        string link = Path.Combine(save, "linked-directory");
        Directory.CreateSymbolicLink(link, Path.Combine(sandbox, "OtherGame"));
        Check(false, save, 1, "symlink inside save");
    }
    catch (UnauthorizedAccessException) { Console.WriteLine("Symlink test skipped (insufficient privileges)"); }
    catch (IOException) { Console.WriteLine("Symlink test skipped (filesystem unsupported)"); }

    Console.WriteLine("SaveFolderGuard checks passed.");
}
finally
{
    // This GUID-named sandbox was created by this test; never touch the real Saves folder.
    Directory.Delete(sandbox, recursive: true);
}

void Check(bool expected, string path, int slot, string label)
{
    bool actual = SaveFolderGuard.TryValidate(root, path, slot, out string accepted);
    if (actual != expected || (actual && !string.Equals(accepted, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)))
        throw new Exception($"{label}: expected {expected}, got {actual}");
}
