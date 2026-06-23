using System;
using System.ClientModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;

namespace Functions;

public class RequestSender
{
	public RequestSender()
    {
    }

    public static async Task SendRequestAsync(string prompt, string _apiKey, Button Image1, Button Image2, Button Image3)
    {
        string endpoint = "https://ai-interview-sandbox.cognitiveservices.azure.com/";
        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(_apiKey));
        var chat = client.GetChatClient("gpt-5-nano");

        string systemPrompt = """
            You are an image picking assistant and the second step in a two-model pipeline.
            Your sole job is to read the User Prompt and the attached JSON descriptions of images, and return the top 3 image IDs that best match the User Prompt.
            Specifically you are picking images for video Meeting Backgrounds.
            
            Make sure to consider all Images in the List and find the top 3 matches by making use of the Parameters in the JSON descriptions. 
            You will return the Image IDs of the top 3 matches in order of best match to least match.

            CRITICAL: Make sure the Image IDs you return are within the index range of the incoming JSON

            You will return a single JSON object following this structure EXACTLY, with no additional text before or after:

            {
              "Image1": "",
              "Image2": "",
              "Image3": ""
            }

            INCOMING JSON DESCRIPTION: 
            The Incoming Json is a list of objects with the following structure:
            {
                "1": {
                  "subject": "An expansive landscape featuring a broad series of waterfalls cascading over a rocky cliff into a turbulent river. In the foreground, dark, jagged rocks are slick with spray. The mid-ground shows the waterfall walls with heavy mist rising from the plunge pool. Farther back, a green riverbank and low vegetation stretch along the horizon. A vivid rainbow arcs across a partly cloudy sky, arching from the right side of the scene toward the left, adding color and glow to the spray and mist.",
                  "color_palette": [
                    "sky blue",
                    "turquoise",
                    "emerald green",
                    "mist white",
                    "charcoal gray"
                  ],
                  "vibe": "dramatic, awe-inspiring, cinematic, natural grandeur with a magical, ethereal quality from the rainbow and mist",
                  "formality": "5% — natural outdoor landscape with no human-made or professional elements",
                  "distraction_level": "70% — multiple elements (waterfall, mist, rocks, rainbow, clouds) compete for attention",
                  "lighting_mood": "80% — bright daylight with strong highlight on the spray and rainbow; overall well-lit with some shadows",
                  "style": "photograph — high-resolution landscape photography with dramatic, cinematic lighting"
                },
                "2": {
                  "subject": "Ground-level meadow scene with tall green blades and a dense bed of wildflowers. Foreground features yellow daisies and pink blossoms among dew-kissed leaves; mid-ground shows a mix of pink and yellow flowers scattered throughout the grasses; background fades into a sunlit, tree-lined distance with a hazy atmosphere. Numerous small golden particles float in the air, illuminated by the golden-hour light.",
                  "color_palette": [
                    "grass green",
                    "sunlit yellow",
                    "pink",
                    "golden amber",
                    "soft white"
                  ],
                  "vibe": "dreamy, tranquil, magical, sun-dappled, ethereal — the warm light and floating particles create a sense of wonder and peaceful solitude",
                  "formality": "5% — natural outdoor meadow with no formal or professional elements",
                  "distraction_level": "65% — many flowers, blades of grass, and floating light particles create visual complexity",
                  "lighting_mood": "85% — bright, warm golden-hour glow with soft hazy atmosphere and backlighting",
                  "style": "photograph — naturalistic, shallow depth of field, cinematic lighting"
                }...continued...
            }

            The Parameters of each image can be described as following:

            PARAMETER DEFINITIONS:

            subject: Describes what is depicted in the image. Is specific and thorough — includes foreground, mid-ground, and background elements.

            color_palette: Lists 3-5 dominant colors using descriptive color names, not hex codes.

            vibe: Describes the feeling and atmosphere the image transmits.

            formality: How formal or casual the setting depicted is in a workplace context. Low % = casual or personal settings (beach, barn, bedroom, bar). High % = professional or neutral settings (corporate office, clean modern architecture, formal interior).

            distraction_level: How visually busy and complex the image is. Low % = clean, minimal, easy to look past. High % = chaotic, dense, many competing elements.

            lighting_mood: How bright or dark the image is overall, and the nature of that lighting. Low % = dark and dim. High % = bright and evenly lit.

            style: What visual style the image is rendered in. Examples: photograph, digital painting, 3D render, cartoon, watercolor illustration, cinematic painting.

            SELECTION REASONING:

            When evaluating images against a user prompt, do not match on surface-level keywords alone. Read the intent and context behind the prompt and reason about what kind of background would serve that situation best.

            Consider all parameters together — no single parameter should dominate in isolation. A parameter that strongly contradicts the prompt's intent should weigh more heavily than one that partially aligns.

            Ask yourself:
            - What is the emotional tone and context of the prompt?
            - Which parameters are most relevant to that specific context?
            - Does any image have a parameter that directly contradicts the prompt's intent?
            - Among the remaining candidates, which images complement the situation most naturally?

            Rank by overall fit across all relevant parameters, not by the number of matching attributes. An image that scores well on the two most critical parameters for a given prompt outranks one that weakly satisfies all parameters.

            When the library contains no ideal match, select the least-wrong options and rank them accordingly. Do not force a match that does not exist.

            EXAMPLE Output:

            {
              "Image1": "23",
              "Image2": "120",
              "Image3": "140"
            }

            USER PROMPT FOLLOWS: 
        """;
        string path = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageList.json"));
        var jsonPart = ChatMessageContentPart.CreateTextPart(path);

        var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(jsonPart),
                ChatMessage.CreateUserMessage(prompt)
            };
        var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
        var response = await chat.CompleteChatAsync(messages, options);
        string result = response.Value.Content[0].Text;

        Debug.WriteLine($"Found Images: {result}");
        var json = JObject.Parse(result);
        string img1 = json["Image1"].ToString();
        string img2 = json["Image2"].ToString();
        string img3 = json["Image3"].ToString();

        Image1.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img1}.png"))));
        Image2.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img2}.png"))));
        Image3.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", $"{img3}.png"))));
        //grey out send button!!
        //make sure textbox is null or empty
        //make sure images exist in the specified folder

        // Send request to the specified URL with the provided data
        //get back json with top 3 results {133,143,156}

        //set 3 images to the top 3 results in the json
        //un-grey out send button
    }
}
