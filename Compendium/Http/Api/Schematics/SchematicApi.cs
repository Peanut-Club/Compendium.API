using System;
using System.IO;
using System.Threading.Tasks;
using Compendium.HttpServer;
using Grapevine;
using Newtonsoft.Json;

namespace Compendium.Http.Api.Schematics {
    [RestResource]
    public class SchematicApi {
        public static string SchematicDir => "/home/scp/.config/EXILED/Configs/MapEditorReborn/Schematics/";
        public static string MusicDir => "/home/scp/.config/EXILED/Configs/Music/";


        [RestRoute("Post", "/api/schematics/upload")]
        public async Task UploadAsync(IHttpContext ctx) {
            try {
                string schematicName = ctx.Request.Headers.Get("X-Name");
                string text = "";
                using (StreamReader reader = new StreamReader(ctx.Request.InputStream)) {
                    text = await reader.ReadToEndAsync();
                }
                if (string.IsNullOrWhiteSpace(schematicName) || string.IsNullOrWhiteSpace(text)) {
                    await ctx.Response.SendResponseAsync("Jméno ani data schematicu nemohou být prázdná!");
                    return;
                }
                string text2 = SchematicDir + schematicName + "/";
                if (Directory.Exists(text2)) {
                    Directory.Delete(text2, true);
                }
                Directory.CreateDirectory(text2);
                Plugin.Info(string.Concat(new string[]
                {
                    "Přijmut schematic '",
                    schematicName,
                    "' z IP ",
                    ctx.GetRealIp(),
                    " (",
                    text2,
                    ")"
                }));
                foreach (SchematicApi.FileData fileData in JsonConvert.DeserializeObject<SchematicApi.FileData[]>(text)) {
                    string text3;
                    if (fileData.Name.EndsWith(".ogg", StringComparison.CurrentCultureIgnoreCase)) {
                        text3 = MusicDir;
                        Plugin.Info(string.Format("Saved music file 'Music/{0}' ({1} bytes)", fileData.Name, fileData.Data.Length));
                    } else {
                        text3 = text2;
                        Plugin.Info(string.Format("Saved schematic file '{0}/{1}' ({2} bytes)", schematicName, fileData.Name, fileData.Data.Length));
                    }
                    File.WriteAllBytes(text3 + fileData.Name, fileData.Data);
                }
                await ctx.Response.SendResponseAsync("OK");
            } catch (Exception ex) {
                Plugin.Error(ex);
                await ctx.Response.SendResponseAsync(ex.Message);
            }
        }


        public class FileData {
            public string Name { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
