using Microsoft.AspNetCore.Mvc;
using SourceAFIS;
using System;
using System.Collections.Generic;
using System.IO;
using DotNetEnv;
using System.Text; 
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

[ApiController]
[Route("api/[controller]")]
public class FingerprintController : ControllerBase
{
    private readonly List<FingerprintTemplateWithFileName> database;
    private readonly double matchThreshold = 40; // Set your match threshold

    public FingerprintController()
    {
        // Load environment variables from .env file
        DotNetEnv.Env.Load();
        string folderPath = Environment.GetEnvironmentVariable("FINGERPRINT_FOLDER_PATH") 
                    ?? throw new Exception("FINGERPRINT_FOLDER_PATH is not set.");

        
        // if (string.IsNullOrEmpty(folderPath))
        // {
        //     throw new Exception("FINGERPRINT_FOLDER_PATH environment variable is not set.");
        // }

        database = LoadFingerprintTemplates(folderPath); // Load your fingerprint templates from the folder
    }

    [HttpPost("match-1-to-1")]
    public IActionResult MatchFingerprints1To1([FromBody] FingerprintMatch1To1Data data)
    {
        try
        {
            // Decode the Base64 strings back into byte arrays
            byte[] probeImage = Convert.FromBase64String(data.Probe);
            byte[] candidateImage = Convert.FromBase64String(data.Candidate);

            // Convert the byte arrays to fingerprintImage
            FingerprintImage probeFingerprintImage = new FingerprintImage(probeImage);
            FingerprintImage candidateFingerprintImage = new FingerprintImage(candidateImage);

            // Process images and create FingerprintTemplate objects
            FingerprintTemplate probeTemplate = new FingerprintTemplate(probeFingerprintImage);
            FingerprintTemplate candidateTemplate = new FingerprintTemplate(candidateFingerprintImage);

            var matcher = new FingerprintMatcher(probeTemplate);
            double score = matcher.Match(candidateTemplate);
            return Ok(new { Score = score, IsMatch = score >= matchThreshold });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("match-1-to-n")]
    public IActionResult MatchFingerprints1ToN([FromBody] FingerprintMatch1ToNData data)
    {
        try
        {
            // Decode the Base64 string back into byte array
            byte[] probeImage = Convert.FromBase64String(data.Probe);

            // Convert the byte array to fingerprintImage
            FingerprintImage probeFingerprintImage = new FingerprintImage(probeImage);

            // Process image and create FingerprintTemplate object
            FingerprintTemplate probeTemplate = new FingerprintTemplate(probeFingerprintImage);

            // Perform 1:N matching
            FingerprintTemplateWithFileName? bestMatch = null;
            double highestScore = 0;
            var matcher = new FingerprintMatcher(probeTemplate);
            foreach (var candidateTemplate in database)
            {
                double score = matcher.Match(candidateTemplate.Template);
                if (score > highestScore)
                {
                    highestScore = score;
                    bestMatch = candidateTemplate;
                }
            }

            // Check if the highest score exceeds your match threshold
            if (highestScore >= matchThreshold && bestMatch != null)
            {
               // return Ok(new { Score = highestScore, IsMatch = true, MatchedTemplate = bestMatch.Template, FileName = bestMatch.FileName });
               return Ok(new { Score = highestScore, IsMatch = true, FileName = bestMatch.FileName }); // Excluded MatchedTemplate for security reasons
            }
            else
            {
                return Ok(new { Score = highestScore, IsMatch = false });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }



private List<FingerprintTemplateWithFileName> LoadFingerprintTemplates(string folderPath)
{
    List<FingerprintTemplateWithFileName> templates = new();

    foreach (var filePath in Directory.GetFiles(folderPath, "*.txt"))
    {
        try
        {
            // Read the Base64 string from the file
            string base64Image = System.IO.File.ReadAllText(filePath).Trim();
            base64Image = base64Image.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            // Convert Base64 to byte array
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            // Load the byte array into an Image object using Argb32
            using var ms = new MemoryStream(imageBytes);
            ms.Position = 0; // Ensure the stream position is at the beginning
            using var decoded = Image.Load<Argb32>(ms);

            // Convert the Image object to a byte array
            using var outputStream = new MemoryStream();
            decoded.SaveAsPng(outputStream);
            byte[] validatedBytes = outputStream.ToArray();

            // Convert to FingerprintTemplate using the byte array
            var fingerprintImage = new FingerprintImage(validatedBytes);
            var template = new FingerprintTemplate(fingerprintImage);

            templates.Add(new FingerprintTemplateWithFileName
            {
                Template = template,
                FileName = Path.GetFileName(filePath)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Failed to load {filePath}: {ex}");
        }
    }

    return templates;
}

private void LogToFile(string message, string originFilePath)
{
    string logDir = @"C:\Users\micha\Documents\Michael\Dev\DotNet\biometric_template";
    string logFile = Path.Combine(logDir, "fingerprint_log.txt");

    string entry = $"[{DateTime.Now}] {Path.GetFileName(originFilePath)}: {message}{Environment.NewLine}";
    System.IO.File.AppendAllText(logFile, entry);
}


private static void CheckImageFormat(byte[] imageBytes)
{
    try
    {
        using var ms = new MemoryStream(imageBytes);
        ms.Position = 0; // Ensure the stream position is at the beginning
        using var image = Image.Load(ms, out IImageFormat format);
        Console.WriteLine($"✅ Image format detected: {format.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Image format detection failed: {ex.Message}");
    }
}


public byte[] ConvertToArgbPng(byte[] originalImage)
{
    using (var image = Image.Load(originalImage))
    {
        image.Mutate(x => x.BackgroundColor(Color.White)); // Avoid transparency
        using (var ms = new MemoryStream())
        {
            image.SaveAsPng(ms, new PngEncoder
            {
                ColorType = PngColorType.RgbWithAlpha
            });
            return ms.ToArray();
        }
    }
}


}

public class FingerprintTemplateWithFileName
{
public FingerprintTemplate? Template { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class FingerprintMatch1To1Data {
    public string Probe { get; set; } = string.Empty;
    public string Candidate { get; set; } = string.Empty;
}

public class FingerprintMatch1ToNData {
    public string Probe { get; set; } = string.Empty;
}
