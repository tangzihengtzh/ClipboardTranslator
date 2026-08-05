using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClipboardTranslator.Api;

public sealed class DeepSeekClient
{
    private readonly HttpClient _httpClient;

    public DeepSeekClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 调用 OpenAI Chat Completions 兼容接口，返回模型消息内容。
    /// </summary>
    public async Task<string> SendChatAsync(
        string baseUrl,
        string apiKey,
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var endpoint = baseUrl.TrimEnd('/') + "/chat/completions";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken)
            .ConfigureAwait(false);

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        int status = (int)response.StatusCode;
        if (status == 401)
            throw new ApiException("API Key 无效，请检查设置。", status);
        if (status == 403)
            throw new ApiException("API 请求被拒绝（403）。", status);
        if (status == 429)
            throw new ApiException("请求过于频繁，请稍后重试。", status);
        if (status >= 500)
            throw new ApiException("翻译服务暂时不可用（5xx）。", status, retryable: true);
        if (!response.IsSuccessStatusCode)
            throw new ApiException($"翻译服务返回错误（HTTP {status}）。", status);

        ChatCompletionResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(body);
        }
        catch (JsonException)
        {
            throw new ApiException("翻译结果格式异常。", status);
        }

        return parsed?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }
}
