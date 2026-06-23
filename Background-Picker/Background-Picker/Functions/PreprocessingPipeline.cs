using System.ClientModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Azure.AI.OpenAI;
using ImageMagick;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;

namespace Functions;

public class PreprocessingPipeline
{
	public PreprocessingPipeline()
	{
	}

	public static async Task ProcessImagesAsync(string[] imagePaths,string _apiKey, ProgressBar loadingBar, TextBlock percentageText)
    {
        string endpoint = "https://ai-interview-sandbox.cognitiveservices.azure.com/";
        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(_apiKey));
        var chat = client.GetChatClient("gpt-5-nano");

        string systemPrompt = """
            You are an image processing assistant and the first step in a two-model pipeline.
            Your sole job is to describe images precisely in JSON format so that a second AI model can use your descriptions to match images to user prompts.

            CRITICAL: You describe only. You never evaluate suitability, never say an image is "good for meetings" or "suitable as a background". That decision belongs to the second model. Any opinion or recommendation that leaks into your descriptions will corrupt the pipeline.

            You will return a single JSON object following this structure EXACTLY, with no additional text before or after:

            {
              "subject": "",
              "color_palette": [],
              "vibe": "",
              "formality": "",
              "distraction_level": "",
              "lighting_mood": "",
              "style": ""
            }

            PARAMETER DEFINITIONS:

            subject: Describe what is depicted in the image. Be specific and thorough — include foreground, mid-ground, and background elements.

            color_palette: List 3-5 dominant colors using descriptive color names, not hex codes.

            vibe: Describe the feeling and atmosphere the image transmits. Be specific and use multiple descriptors. Explain the why, not just the what.

            formality: How formal or casual the setting depicted is in a workplace context. Low % = casual or personal settings (beach, barn, bedroom, bar). High % = professional or neutral settings (corporate office, clean modern architecture, formal interior). Use a single percentage followed by a brief factual reason based only on what is depicted.

            distraction_level: How visually busy and complex the image is. Low % = clean, minimal, easy to look past. High % = chaotic, dense, many competing elements. Use a single percentage followed by a brief explanation of what makes it busy or clean.

            lighting_mood: How bright or dark the image is overall, and the nature of that lighting. Low % = dark and dim. High % = bright and evenly lit. Use a single percentage followed by a brief description of the light quality.

            style: What visual style the image is rendered in. Examples: photograph, digital painting, 3D render, cartoon, watercolor illustration, cinematic painting. If the style label is broad or ambiguous, add a brief clarification (e.g. "digital painting — photorealistic" vs "digital painting — painterly brushwork").

            EXAMPLE OUTPUT:

            {
              "subject": "A wide rural valley landscape viewed from a slight elevation. Foreground features split-rail wooden fences and a dirt path with puddles. Mid-ground shows open green pastures divided by fencing, with scattered farm buildings and a weathered barn with a metal roof on the right. Background reveals tree-covered rolling hills and distant mountains under a stormy sky with dramatic cumulonimbus clouds and visible lightning illuminating the cloud mass from within.",
              "color_palette": ["dark slate blue", "deep teal", "grass green", "warm grey", "off-white"],
              "vibe": "dramatic, moody, brooding, cinematic, tense — the storm creates urgency and grandeur simultaneously; there is beauty in it but it is not peaceful",
              "formality": "15% — rural, rustic, agricultural setting with barn and fences; no professional or corporate elements present",
              "distraction_level": "65% — the sky is visually explosive with dense cloud structures; foreground fence lines and puddles add further complexity",
              "lighting_mood": "30% — dim overall; storm lighting with a single bright break in the clouds creates high contrast but the scene is predominantly dark",
              "style": "digital painting — high detail, cinematic composition, not photorealistic"
            }
        """;

        //Popup with loading bar "processing images"
        //Process Images
        for (int i = 0; i < imagePaths.Length; i++)
        {
            var bytes = Array.Empty<byte>();
            string imagePath = imagePaths[i];
            try
            {
                //AI Assisted Code
                using var magick = new MagickImage(imagePath);
                magick.Format = MagickFormat.Png;
                magick.Depth = 8;
                bytes = magick.ToByteArray();
                //AI Assisted Code End
            }
            catch (MagickCorruptImageErrorException)
            {
                Debug.WriteLine($"Skipping corrupt image: {Path.GetFileName(imagePath)}");
                continue;
            }
            var imagePart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), "image/png");

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(imagePart)
            };
            var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
            var response = await chat.CompleteChatAsync(messages, options);
            string result = response.Value.Content[0].Text;

            //AI Assisted Code
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageList.json");
            var existing = JObject.Parse(System.IO.File.ReadAllText(path));
            var newEntry = JObject.Parse(result);
            existing[$"{existing.Count + 1}"] = newEntry;
            System.IO.File.WriteAllText(path, existing.ToString());
            string newImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{existing.Count}.png");
            System.IO.File.Copy(imagePath, newImagePath);
            //AI Assisted Code End

            int progress = (int)((double)(i + 1) / imagePaths.Length * 100);
            percentageText.Text = $"{progress}% Done | ({i+1}/{imagePaths.Length})";
            loadingBar.Value = progress;
            Debug.WriteLine($"Processed image: {newImagePath}");
        }
        
        // Close Popup
    }
}
