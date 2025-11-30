using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;

namespace agent_study
{
    public static class APIKey
    {
        //APIKey.Use("qwen-max");      //结构化×，调用工具√
        //APIKey.Use("kimi");          //结构化√，调用工具×
        //APIKey.Use("glm-4.6");       //结构化×，调用工具√
        //APIKey.Use("deepseek");      //结构化×，调用工具√
        //APIKey.Use("doubao");        //结构化√，调用工具√
        //APIKey.Use("gpt-5");         //结构化×，调用工具√
        //APIKey.Use("gemini-3-pro");  //结构化×，调用工具√
        //APIKey.Use("cluade");        //结构化×，调用工具√
        private static readonly Dictionary<string, ModelProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["qwen-max"] = new ModelProfile
            {
                Name = "qwen-max",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "qwen-max"
            },
            ["qwen-vl"] = new ModelProfile
            {
                Name = "qwen-vl",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "qwen-vl-max-latest"
            },
            ["kimi"] = new ModelProfile
            {
                Name = "kimi",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://api.moonshot.cn/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "kimi-k2-0905-preview"
            },
            ["gpt-5"] = new ModelProfile
            {
                Name = "gpt-5-all",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://api.whatai.cc/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "gpt-5-all"
            },
            ["deepseek"] = new ModelProfile
            {
                Name = "deepseek-v3-2-exp",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://api.whatai.cc/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "deepseek-v3-2-exp"
            },
            ["cluade"] = new ModelProfile
            {
                Name = "claude-sonnet-4-5-20250929",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://api.whatai.cc/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "claude-sonnet-4-5-20250929"
            },
            ["gemini-3-pro"] = new ModelProfile
            {
                Name = "gemini-3-pro-preview",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://api.whatai.cc/v1") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "gemini-3-pro-preview"
            },
            ["doubao"] = new ModelProfile
            {
                Name = "doubao",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "doubao-seed-1-6-251015"
            },
            ["glm-4.6"] = new ModelProfile
            {
                Name = "glm-4.6",
                ClientOptions = new OpenAIClientOptions { Endpoint = new Uri("https://open.bigmodel.cn/api/paas/v4") },
                Credential = new ApiKeyCredential("sk-xxxxxx"),
                Model = "glm-4.6"
            }
        };

        // 当前激活的配置，默认使用 qwen-max
        private static ModelProfile _active = Profiles["qwen-max"];

        // 暴露当前激活配置的属性：其他类直接调用这三个属性即可
        public static OpenAIClientOptions ClientOptions => _active.ClientOptions;

        public static ApiKeyCredential Credential => _active.Credential;
        public static string Model => _active.Model;

        public static void RegisterOrUpdate(ModelProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Name)) throw new ArgumentException("Profile name is required", nameof(profile));
            Profiles[profile.Name] = profile;
        }

        public static bool Use(string profileName)
        {
            if (Profiles.TryGetValue(profileName, out var profile))
            {
                _active = profile;
                return true;
            }
            return false;
        }
    }

    // 模型配置结构：描述一套模型的连接信息（名称、Endpoint、密钥、模型名）
    public class ModelProfile
    {
        // OpenAI 兼容客户端配置（主要设置服务地址 Endpoint）
        public OpenAIClientOptions ClientOptions { get; init; } = new OpenAIClientOptions();

        // 访问服务需要的密钥
        public ApiKeyCredential? Credential { get; init; }

        // 使用的具体模型名称
        public string Model { get; init; } = string.Empty;

        // 配置名称，用于在注册表中区分不同模型
        public string Name { get; init; } = string.Empty;
    }
}