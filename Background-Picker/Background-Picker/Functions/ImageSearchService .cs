using System.ClientModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Azure.AI.OpenAI;
using Helpers;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Functions;

public class ImageSearchService
{
    public static async Task SendRequestAsync(string prompt, string _apiKey, Button Image1, Button Image2, Button Image3)
    {
        //parameters for embeddingresults -> final selection using gpt-5-nano
        int minItems = 10;
        int maxItems = 50;
        double threshold = 0.55;

        string imageListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageList.json");
        string imageVectorsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageVectors.json");


        string endpoint = "https://ai-interview-sandbox.cognitiveservices.azure.com/";
        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(_apiKey));
        var chat = client.GetChatClient("gpt-5-nano");
        var embedder = client.GetEmbeddingClient("text-embedding-3-large");


        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(AIConfig.UserToVectorSystemPrompt),
            ChatMessage.CreateUserMessage(prompt)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "prompt_categorization",
                AIConfig.ImageDescriptionSchema,
                jsonSchemaIsStrict: true)
        };

        var response = await chat.CompleteChatAsync(messages, options);
        string responseText = response.Value.Content[0].Text;

        JObject newEntry = JObject.Parse(responseText);

        Debug.WriteLine($"New Entry: {newEntry.ToString()}");

        List<string> queryFieldTexts =
            [
                $"subject: {newEntry["subject"]}",
                $"color_palette: {string.Join(", ", newEntry["color_palette"]!)}",
                $"vibe: {newEntry["vibe"]}",
                $"formality: {newEntry["formality"]}",
                $"distraction_level: {newEntry["distraction_level"]}",
                $"lighting_mood: {newEntry["lighting_mood"]}",
                $"style: {newEntry["style"]}"
            ];

        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
        var embeddingResponse = await embedder.GenerateEmbeddingsAsync(queryFieldTexts, embeddingOptions);
        JObject imageVectors = JObject.Parse(File.ReadAllText(imageVectorsPath));

        var RatedList = new Dictionary<string, double>();

        foreach (var i in imageVectors.Properties())
        {
            double score = 0;
            string imageId = i.Name;
            JObject fields = (JObject)i.Value;
            List<JProperty> fieldList = fields.Properties().ToList();

            for (int f = 0; f < fieldList.Count; f++)
            {
                float[] storedVector = fieldList[f].Value.ToObject<float[]>() ?? [];
                float[] queryVector = embeddingResponse.Value[f].ToFloats().ToArray();
                score += CosineSimilarity.Calculate(queryVector, storedVector);
            }

            RatedList.Add(imageId, (score/7));
        }


        List<KeyValuePair<string, double>> SortedRatedList = RatedList.OrderByDescending(x => x.Value)
            .Where(x => x.Value >= threshold)
            .Take(maxItems)
            .ToList();
        if (SortedRatedList.Count < minItems)
        {
            SortedRatedList = RatedList.OrderByDescending(x => x.Value)
                .Take(minItems)
                .ToList();
        }

        Debug.WriteLine($"Top List: {JArray.FromObject(SortedRatedList).ToString()}");


        JObject existingImageList = JObject.Parse(File.ReadAllText(imageListPath));
        var ImageSelection = SortedRatedList.Select(x => new { Id = x.Key, Entry = existingImageList[x.Key] }).ToList();

        var jsonPart = ChatMessageContentPart.CreateTextPart(JArray.FromObject(ImageSelection).ToString());

        messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(AIConfig.ImagesToTop3SystemPrompt),
            ChatMessage.CreateUserMessage(jsonPart),
            ChatMessage.CreateUserMessage(prompt)
        };
        options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "top3_picker",
                AIConfig.Top3PickerSchema,
                jsonSchemaIsStrict: true)
        };
        response = await chat.CompleteChatAsync(messages, options);
        responseText = response.Value.Content[0].Text;

        Debug.WriteLine($"Found Images: {responseText}");
        JObject top3selectionJSON = JObject.Parse(responseText);
        string img1 = top3selectionJSON["Image1"].ToString();
        string img2 = top3selectionJSON["Image2"].ToString();
        string img3 = top3selectionJSON["Image3"].ToString();

        Image1.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img1}.png"))));
        Image1.Tag = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img1}.png");
        Image2.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img2}.png"))));
        Image2.Tag = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img2}.png");
        Image3.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img3}.png"))));
        Image3.Tag = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img3}.png");
    }
}
