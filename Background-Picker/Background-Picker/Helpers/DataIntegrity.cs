using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Helpers
{
    public class DataIntegrity
    {
        //Crosscheck and align all 3 Files (images themself, list of descriptions, list of vectors), that hold an Index of the ImageData
        public static void SyncImageData()
        {
            string imageListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageList.json");
            string imageVectorsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ImageVectors.json");
            string imageFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images");

            JObject imageList = JObject.Parse(File.ReadAllText(imageListPath));
            JObject imageVectors = JObject.Parse(File.ReadAllText(imageVectorsPath));

            var fileSet = new HashSet<int>(Directory.GetFiles(imageFolderPath)
                .Select(x => int.Parse(Path.GetFileNameWithoutExtension(x))));
            var listSet = new HashSet<int>(imageList.Properties().Select(p => int.Parse(p.Name)));
            var vectorSet = new HashSet<int>(imageVectors.Properties().Select(p => int.Parse(p.Name)));

            var allIds = new HashSet<int>(fileSet);
            allIds.UnionWith(listSet);
            allIds.UnionWith(vectorSet);

            foreach (int id in allIds)
            {
                if (fileSet.Contains(id) && listSet.Contains(id) && vectorSet.Contains(id))
                    continue;

                imageList.Remove($"{id}");
                imageVectors.Remove($"{id}");
                string imagePath = Path.Combine(imageFolderPath, $"{id}.png");
                File.Delete(imagePath);
                Debug.WriteLine($"DATA ALIGNMENT: Removed {id} due to missing entry");
            }

            File.WriteAllText(imageListPath, imageList.ToString());
            File.WriteAllText(imageVectorsPath, imageVectors.ToString());
            Debug.WriteLine("Finished Data Alignment");
        }
    }
}
