using System.ClientModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Azure.AI.OpenAI;
using ImageMagick;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Helpers;

namespace Functions;

public class PreprocessingPipeline
{
    public static async Task ProcessImagesAsync(string[] imagePaths, string _apiKey, ProgressBar loadingBar, TextBlock percentageText)
    {
        string imageListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageList.json");
        JObject existingImageList = JObject.Parse(File.ReadAllText(imageListPath));
        string imageVectorsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageVectors.json");

        string endpoint = "https://ai-interview-sandbox.cognitiveservices.azure.com/";
        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(_apiKey));
        var chat = client.GetChatClient("gpt-5.4");
        var embedder = client.GetEmbeddingClient("text-embedding-3-large");


        for (int i = 0; i < imagePaths.Length; i++)
        {
            var bytes = Array.Empty<byte>();
            string imagePath = imagePaths[i];

            try
            {
                new MagickImageInfo(imagePath);
                bytes = File.ReadAllBytes(imagePath);
            }
            catch (MagickCorruptImageErrorException)
            {
                Debug.WriteLine($"Skipping corrupt image: {Path.GetFileName(imagePath)}");
                continue;
            }

            var ApiRequestImagePart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), "image/png");

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(AIConfig.ImageToEntrySystemPrompt),
                ChatMessage.CreateUserMessage(ApiRequestImagePart)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "image_description",
                AIConfig.ImageDescriptionSchema,
                jsonSchemaIsStrict: true)
            };

            var response = await chat.CompleteChatAsync(messages, options);
            string result = response.Value.Content[0].Text;

            JObject newImageListEntry = JObject.Parse(result);

            int newImageIndex = existingImageList.Properties().Max(p => int.Parse(p.Name)) + 1;

            existingImageList[$"{newImageIndex}"] = newImageListEntry;
            File.WriteAllText(imageListPath, existingImageList.ToString());
            string newImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{newImageIndex}.png");
            File.Copy(imagePath, newImagePath);

            List<string> fieldTexts =
            [
                $"subject: {newImageListEntry["subject"]}",
                $"color_palette: {string.Join(", ", newImageListEntry["color_palette"]!)}",
                $"vibe: {newImageListEntry["vibe"]}",
                $"formality: {newImageListEntry["formality"]}",
                $"distraction_level: {newImageListEntry["distraction_level"]}",
                $"lighting_mood: {newImageListEntry["lighting_mood"]}",
                $"style: {newImageListEntry["style"]}"
            ];

            var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
            var embeddingResponse = await embedder.GenerateEmbeddingsAsync(fieldTexts, embeddingOptions);

            float[] subjectVec = embeddingResponse.Value[0].ToFloats().ToArray();
            float[] color_paletteVec = embeddingResponse.Value[1].ToFloats().ToArray();
            float[] vibeVec = embeddingResponse.Value[2].ToFloats().ToArray();
            float[] formalityVec = embeddingResponse.Value[3].ToFloats().ToArray();
            float[] distraction_levelVec = embeddingResponse.Value[4].ToFloats().ToArray();
            float[] lighting_moodVec = embeddingResponse.Value[5].ToFloats().ToArray();
            float[] styleVec = embeddingResponse.Value[6].ToFloats().ToArray();

            JObject vectorEntry = new JObject
            {
                ["subject"] = new JArray(subjectVec),
                ["color_palette"] = new JArray(color_paletteVec),
                ["vibe"] = new JArray(vibeVec),
                ["formality"] = new JArray(formalityVec),
                ["distraction_level"] = new JArray(distraction_levelVec),
                ["lighting_mood"] = new JArray(lighting_moodVec),
                ["style"] = new JArray(styleVec)
            };

            JObject existingImageVectors = JObject.Parse(File.ReadAllText(imageVectorsPath));
            existingImageVectors[$"{newImageIndex}"] = vectorEntry;
            File.WriteAllText(imageVectorsPath, existingImageVectors.ToString());

            int progress = (int)((double)(i + 1) / imagePaths.Length * 100);
            percentageText.Text = $"{progress}% Done | ({i+1}/{imagePaths.Length})";
            loadingBar.Value = progress;

            Debug.WriteLine($"Processed image: {newImagePath}");
        }        
    }

}

