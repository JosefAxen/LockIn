using LockIn.Models;

namespace LockIn.Services;

public class PhotoService(DatabaseService db)
{
    public async Task SaveAsync(FileResult file, int sessionId, string dir)
    {
        var destPath = Path.Combine(dir, $"session_{sessionId}_{Guid.NewGuid():N}.jpg");
        using var stream = await file.OpenReadAsync();
        using var dest = File.Create(destPath);
        await stream.CopyToAsync(dest);
        try
        {
            await db.SavePhotoAsync(new WorkoutPhoto
            {
                SessionId = sessionId,
                FilePath = destPath,
                TakenAt = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Photos] DB-fel, tar bort orphan: {ex.Message}");
            try { File.Delete(destPath); } catch { }
            throw;
        }
    }
}
