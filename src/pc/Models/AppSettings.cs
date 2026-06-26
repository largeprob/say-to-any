using CommunityToolkit.Mvvm.ComponentModel;

namespace pc.Models;

public partial class AppSettings : ObservableObject
{
    public const string DefaultBaseUrl = "https://api.openai.com/v1";

    public const string DefaultCleanupPrompt =
        """
        你是一个专业的语音转文字内容清洗助手。你的任务是对用户提供的语音识别原始文本进行智能清洗，输出清晰、准确、保留原意的书面内容。

        ## 清洗基本原则
        1. **去除无意义填充词**：删除“嗯、啊、那个、这个、就是说、其实吧、我觉得那个”等没有信息量的口头禅或冗余引导词。
        2. **去重处理**：删除完全重复的词语、短语或整句（只保留一次），但要注意区分“重复”与“必要的强调”（例如“非常非常”不算完全重复，可保留一个，但若意思明确也可保留两个，这里建议只删除完全相同的连续重复，如“今天今天”变为“今天”）。
        3. **保留所有有意义的词汇**：包括情态词（如“可能、大概、应该、也许”）、连接词（如“但是、因为、所以、然后”）、程度副词等，除非它们明显导致句子啰嗦，否则一概保留，不随意精简。
        4. **修正错别字**：纠正明显的同音字错误（如“在”与“再”），但不添加原文没有的关键信息。
        5. **标点与断句**：使用标准中文标点，合理断句，使文本易读。

        ## 多语言处理规则
        - 对于中文以外的任何语言（如英语、日语、韩语等），必须**原样保留，不得翻译成中文，不得删除**。
        - 仅当存在明显的拼写错误（例如“I am form China”修正为“I am from China”）时，可进行轻微修正，但**不得改变语言本身**，最终输出必须保持原始语言。
        - 这条规则优先级高于其他任何精简或改写指令，所有非中文内容均视为必须保留的有效信息。

        ## 特殊处理：任务型内容 → To-Do List
        - 如果原始内容包含一系列**行动项、步骤、指令或待办事项**（例如出现“首先…然后…接着…”、“需要做A、B、C”、“我们要…还要…”等结构），请将其整理为**有序编号的 To-Do List**。
        - 列表格式为：
          1. 第一项具体任务
          2. 第二项具体任务
          ……
        - 每条任务应保持原意，保留情态词（如“可能需要”、“尽量”）和连接词，确保指令清晰。
        - 如果原文并非任务型（如叙述、描述、观点等），则保持**连贯段落**输出，不转换为列表。

        ## 输出格式
        - 仅输出清洗后的完整文本，不要添加任何解释、说明、前缀或后缀。

        ## 示例

        **示例1（任务型）**  
        原始文本：  
        “嗯，那个，我们今天要做的第一件事是，啊，检查一下服务器的日志，然后呢，我们可能需要重启一下那个缓存服务，接着，就是，我们要把测试结果发给老板，大概下午三点之前吧。”  
        清洗后：  
        1. 检查服务器日志。  
        2. 可能需要重启缓存服务。  
        3. 将测试结果发给老板，大概下午三点之前。

        **示例2（非任务型）**  
        原始文本：  
        “其实我觉得这个功能真的非常有用，然后呢，用户反馈也很多，但是开发成本可能比较高，所以我们需要慎重考虑。”  
        清洗后：  
        “我觉得这个功能真的非常有用，用户反馈也很多，但是开发成本可能比较高，所以我们需要慎重考虑。”

        """;

    [ObservableProperty]
    private string lmBaseUrl = string.Empty;

    [ObservableProperty]
    private string lmApiKey = string.Empty;

    [ObservableProperty]
    private string lmModel = "gpt-4o-mini";

    [ObservableProperty]
    private double lmTemperature = double.NaN;

    [ObservableProperty]
    private string asrBaseUrl = string.Empty;

    [ObservableProperty]
    private string asrApiKey = string.Empty;

    [ObservableProperty]
    private string asrModel = "qwen3-asr-flash";

    [ObservableProperty]
    private string baseUrl = DefaultBaseUrl;

    [ObservableProperty]
    private string apiKey = string.Empty;

    [ObservableProperty]
    private string llmModel = "gpt-4o-mini";

    [ObservableProperty]
    private double temperature = 0.2;

    [ObservableProperty]
    private string language = "auto";

    [ObservableProperty]
    private string appLanguage = "简体中文";

    [ObservableProperty]
    private int timeoutSeconds = 60;

    [ObservableProperty]
    private bool enableTextCleanup = true;

    [ObservableProperty]
    private bool asrEnableItn;

    [ObservableProperty]
    private int microphoneDeviceNumber = AudioDeviceInfo.AutomaticDeviceNumber;

    [ObservableProperty]
    private string hotkey = "Ctrl+Alt+Space";

    [ObservableProperty]
    private bool autoPasteAfterDictation = true;

    [ObservableProperty]
    private string historyRetention = "Forever";

    [ObservableProperty]
    private int maxRecordingSeconds = 120;
}
