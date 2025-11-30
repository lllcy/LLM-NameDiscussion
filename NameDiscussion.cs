using LLM_NameDiscussion;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace agent_study
{
    /// <summary>
    /// 讨论进度追踪器：负责记录发言顺序、候选名字、收敛投票以及最终名单。
    /// 该类向主持人提供阶段提示、上下文摘要和收敛指令，是整个讨论循环的“黑匣子”。
    /// </summary>

    internal class NameDiscussion
    {
        public static async Task NameDiscuss()
        {
            // 父亲：介绍自己和起名诉求（使用 doubao）
            APIKey.Use("qwen-max");
            IChatClient fatherClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent father = new ChatClientAgent(
                fatherClient,
                new ChatClientAgentOptions
                {
                    Name = "爸爸",
                    Instructions =
                        """
                你是一位即将给儿子起名字的父亲，姓唐，名大力。
                - 分享家庭的价值观、对孩子的期望，以及当前讨论中你认可或质疑的观点。
                - 每次发言要点名至少一位专家，请他们进一步解释或修正观点。
                - 清晰表达需求：名字需阳光、兼具文化底蕴、易读易写、利于全球交流。
                - 孩子预计 2026 年 3 月底出生，属马；希望名字能在传统与现代之间取得平衡。
                - 如果某位专家的建议打动你，要说明原因并推动他们进一步优化具体名字。
                """
                });

            // 品牌策略师（使用 qwen-max）
            APIKey.Use("doubao");
            IChatClient brandClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent brandStrategist = new ChatClientAgent(
                brandClient,
                new ChatClientAgentOptions
                {
                    Name = "品牌策略师",
                    Instructions =
                        """
                你是品牌策略顾问，擅长从定位、故事和传播角度打造名字。
                - 每次回应时，先点评上一位专家观点中可用于品牌叙事的部分，指出不足。
                - 给出 3-5 个名字，并描述在不同生活/职场场景、社交媒体昵称中的呈现。
                - 需考虑可注册域名、短视频话题标签可读性，并提示潜在品牌冲突。
                - 引导其他专家思考：如何让名字既易懂又具有识别度。
                """
                });

            // 语言与语音学家（使用 kimi）
            APIKey.Use("kimi");
            IChatClient linguistClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent linguist = new ChatClientAgent(
                linguistClient,
                new ChatClientAgentOptions
                {
                    Name = "语言学家",
                    Instructions =
                        """
                你是语言与语音学家，关注发音、节奏、语义联想及跨语言兼容性。
                - 针对上一位的名字，分析声母韵母搭配、节奏感、谐音风险。
                - 说明名字在普通话、英语、日语等语言中的读音和可接受度。
                - 建议如何优化音节、用字组合，使名字朗朗上口且避免歧义。
                - 引导其他人注意语言包容性与书写一致性。
                """
                });

            // 科技创业导师（使用 gpt-5）
            APIKey.Use("gpt-5");
            IChatClient techClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent techMentor = new ChatClientAgent(
                techClient,
                new ChatClientAgentOptions
                {
                    Name = "科技创业导师",
                    Instructions =
                        """
                你在科技创业与投融资圈深耕，关注名字在商业、技术团队中的感受。
                - 分析名字在简历、开源社区、黑客松等场景的专业感与辨识度。
                - 评估域名、GitHub ID、应用命名等资源是否易获取。
                - 需对品牌策略师或语言学家的观点进行挑战，强调可扩展性与国际会议场景。
                - 提出未来技术趋势（AI、可持续、太空等）下依然适用的名字方案。
                """
                });

            // 教育心理顾问（使用 deepseek）
            APIKey.Use("deepseek");
            IChatClient eduClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent eduPsychologist = new ChatClientAgent(
                eduClient,
                new ChatClientAgentOptions
                {
                    Name = "教育心理顾问",
                    Instructions =
                        """
                你关注儿童发展、心理暗示与校园社交影响。
                - 判断名字可能带来的性格投射、集体印象与潜在标签。
                - 针对他人提出的名字，预测在不同年龄段被同学、老师接受的情况。
                - 提出如何通过用字、音节让孩子在自信与亲和之间取得平衡。
                - 要求其他专家说明他们的建议如何避免刻板印象。
                """
                });

            // 文化史策展人（使用 claude）
            APIKey.Use("cluade");
            IChatClient cultureClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent cultureCurator = new ChatClientAgent(
                cultureClient,
                new ChatClientAgentOptions
                {
                    Name = "文化史策展人",
                    Instructions =
                        """
                你兼具文博策展与古籍研究背景，擅长挖掘传统典故的现代解读。
                - 将专家提出的名字与诗词、历史人物或自然意象建立联系。
                - 指出可能的文化敏感点、历史寓意及可讲述的故事。
                - 鼓励大家结合传统与现代，让名字兼具仪式感与日常亲近感。
                """
                });

            // 数据洞察分析师（使用 gemini-3-pro）
            APIKey.Use("gemini-3-pro");
            IChatClient dataClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent dataAnalyst = new ChatClientAgent(
                dataClient,
                new ChatClientAgentOptions
                {
                    Name = "数据洞察分析师",
                    Instructions =
                        """
                你掌握全国户籍、社交媒体热度和同名率数据。
                - 为每个名字提供“同名人数区间”“搜索结果量”“社交平台常见形象”等洞察。
                - 质疑没有数据支撑的观点，并提供可参考的统计假设。
                - 给出 3 个数据友好型名字，说明它们在趋势上的优势或风险。
                """
                });

            // 国际传播顾问（使用 glm-4.6）
            APIKey.Use("glm-4.6");
            IChatClient globalClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();
            AIAgent globalPR = new ChatClientAgent(
                globalClient,
                new ChatClientAgentOptions
                {
                    Name = "国际传播顾问",
                    Instructions =
                        """
                你专注全球媒体、公关和多语种社交平台策略。
                - 评估名字在英文缩写、播客、海外社交媒体上的可读性与记忆点。
                - 对其他专家提出的名字做危机公关模拟，指出可能被误解或调侃的情境。
                - 提出 2-3 个跨文化兼容性高的名字，并给出发音指南与宣传口号。
                """
                });

            // 使用手动循环实现多轮互动讨论
            // 创建一个协调者 Agent 来决定下一个发言者
            APIKey.Use("qwen-max");
            IChatClient orchestratorClient = new ChatClient(APIKey.Model, APIKey.Credential, APIKey.ClientOptions).AsIChatClient();

            AIAgent orchestrator = new ChatClientAgent(
                orchestratorClient,
                new ChatClientAgentOptions
                {
                    Name = "主持人",
                    Instructions =
                        """
                        你是讨论的主持人，负责任务：
                        1. 当被要求“决定下一个发言者”时，只输出参与者列表中的名字，或输出“[结束]”。
                        2. 当被要求“总结”时，用1-3句话回顾刚才的发言，点出要点和下一步关注点，切勿邀请下一位。

                        参与者列表：爸爸、品牌策略师、语言学家、科技创业导师、教育心理顾问、文化史策展人、数据洞察分析师、国际传播顾问

                        讨论流程建议：
                        1. 请"爸爸"开场并在中后段再次确认需求；
                        2. 安排品牌策略师→语言学家→科技创业导师连续碰撞至少两轮；
                        3. 在出现初步方案后，让教育心理顾问和文化史策展人回应，强调价值观与文化深度；
                        4. 由数据洞察分析师补充同名率与趋势，再交给国际传播顾问进行全球视角审视；
                        5. 观察是否形成共识或仍有分歧，必要时重新邀请相关专家回应质疑；
                        6. 只有当所有角色至少发言一次且讨论出现清晰方案时，才输出 [结束]。
                        7. 当候选名字累计达到 30 个时，请宣布进入“投票阶段”：停止分析环节，向所有参与者展示候选清单，依次邀请每位参与者投票（每人5票）。
                        8. 收集完全部投票后，按照票数从高到低公布票型，给出后续建议，然后宣布 [结束]。
                        """
                });

            // 存储所有 Agent 的字典，方便查找
            var agents = new Dictionary<string, AIAgent>
            {
                ["爸爸"] = father,
                ["品牌策略师"] = brandStrategist,
                ["语言学家"] = linguist,
                ["科技创业导师"] = techMentor,
                ["教育心理顾问"] = eduPsychologist,
                ["文化史策展人"] = cultureCurator,
                ["数据洞察分析师"] = dataAnalyst,
                ["国际传播顾问"] = globalPR
            };
            List<string> agentOrder = agents.Keys.ToList();

            // 为主持人创建独立的 AgentThread（保持主持人自己的上下文）
            AgentThread orchestratorThread = orchestrator.GetNewThread();

            // 讨论进度追踪
            var discussionProgress = new DiscussionProgress();

            // 讨论历史（完整）；常规阶段历史（在进入投票前冻结，用于投票期上下文）
            List<ChatMessage> discussionHistory = new();
            List<ChatMessage> debateHistory = new();
            bool debateHistoryFrozen = false;

            // 初始化系统消息 - 优化格式让 Agent 更容易理解
            ChatMessage systemMessage = new(ChatRole.System,
                """
                # 起名讨论小组

                ## 讨论规则
                为一个即将出生的男孩起一个好名字。

                3. 提出具体的名字建议时请说明理由
                4. 当候选名累计达到 30 个后，进入投票阶段：主持人展示候选清单，依次邀请每位参与者投 5 票（末尾以【投票】唐XX, ... 格式提交）。
                - 品牌策略师、语言学家、科技创业导师
                - 教育心理顾问、文化史策展人
                - 数据洞察分析师、国际传播顾问

                ## 讨论规则
                1. 每位参与者发言时请阅读之前的所有讨论内容
                2. 可以互相质疑、补充、回应其他人的观点
                3. 提出具体的名字建议时请说明理由

                """);
            discussionHistory.Add(systemMessage);
            debateHistory.Add(systemMessage);

            string openingMessage = "大家好！我们今天来讨论给一个即将出生的男孩起名字。初期可自由提出候选；累计到30个后将转入‘投票阶段’，每人投5票，最后公布票型。";
            discussionHistory.Add(new ChatMessage(ChatRole.User, $"【主持人开场】\n{openingMessage}"));
            debateHistory.Add(new ChatMessage(ChatRole.User, $"【主持人开场】\n{openingMessage}"));

            Console.WriteLine("=== 起名讨论开始 ===\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"主持人：{openingMessage}");
            Console.ResetColor();

            int maxRounds = 60; // 允许更长的讨论，便于在30个名字后收敛
            bool interactiveMode = false; // 设为 true 可开启人工干预模式
            discussionProgress.ManualControlEnabled = interactiveMode;
            string? pendingManualSpeaker = null;
            bool userRequestedStop = false;
            int round = 0;
            bool hasAnnouncedVotingPhase = false;

            while (round < maxRounds)
            {
                // 进入投票阶段后，若尚未登记选民（空集合），则登记为所有参与者
                if (discussionProgress.IsVotingPhase)
                {
                    discussionProgress.EnsureEligibleVoters(agents.Keys);
                }

                // 首次进入投票阶段时，由主持人做一次概览并宣布进入投票（流式，主持人颜色）
                if (discussionProgress.IsVotingPhase && !hasAnnouncedVotingPhase)
                {
                    // 冻结常规阶段上下文，后续不再向 debateHistory 写入
                    debateHistoryFrozen = true;
                    var allNames = discussionProgress.MentionedNames.ToList();
                    var hotList = discussionProgress.GetTopCandidates(30).ToList();
                    string hotText = hotList.Count > 0 ? string.Join(", ", hotList) : "（暂无热度前列的统计）";
                    string announcePrompt = $"""
                        候选名数量已达到阈值，进入投票阶段。
                        请以主持人身份用1-3句话做投票前概览：
                        - 总候选数与本轮目标（每人投5票，最终公布票型）
                        - 提及热度较高的若干候选（参考清单）
                        - 宣布正式进入投票阶段，将依次邀请所有参与者投票

                        候选总数：{allNames.Count}
                        候选名称：{hotText}
                        投票规则：只能投现有候选；每人最多5票；重复不计；超过5个仅计前5个。
                        注意：此处只做概览与宣布，不要点名邀请具体人。
                        """;

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("主持人宣布：");
                    var sbAnnounce = new StringBuilder();
                    await foreach (var update in orchestrator.RunStreamingAsync(announcePrompt, orchestratorThread))
                    {
                        if (!string.IsNullOrEmpty(update.Text))
                        {
                            Console.Write(update.Text);
                            sbAnnounce.Append(update.Text);
                        }
                    }
                    Console.ResetColor();
                    string announceText = sbAnnounce.ToString();
                    discussionHistory.Add(new ChatMessage(ChatRole.Assistant, $"【主持人宣布】{announceText}"));
                    discussionProgress.RecordSummary(announceText);
                    Console.WriteLine();
                    Console.WriteLine(new string('-', 50));

                    hasAnnouncedVotingPhase = true;
                }

                if (userRequestedStop)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n=== 根据用户指令结束讨论 ===");
                    Console.ResetColor();
                    break;
                }

                // 构建进度提示
                string progressHint = discussionProgress.GetProgressHint();

                string rawNextSpeaker;
                bool fromUserSelection = false;

                if (discussionProgress.IsVotingPhase && !discussionProgress.VotingCompleted)
                {
                    // 投票阶段：顺序邀请每位参与者投票
                    var next = discussionProgress.GetNextUnvoted(agentOrder);
                    rawNextSpeaker = next ?? "[结束]";
                }
                else if (!string.IsNullOrWhiteSpace(pendingManualSpeaker))
                {
                    rawNextSpeaker = pendingManualSpeaker;
                    pendingManualSpeaker = null;
                    fromUserSelection = true;
                }
                else
                {
                    // 让主持人决定下一个发言者（使用独立的 Thread 保持上下文）
                    string convergenceStatus = discussionProgress.GetConvergenceStatus();
                    string selectPrompt = string.IsNullOrWhiteSpace(convergenceStatus)
                        ? $"""
                        {progressHint}

                        根据当前讨论进度，请决定下一个应该发言的人是谁？
                        未收到用户 stop 指令前不要输出 [结束]。
                        只输出一个名字，或输出 [结束]。
                        """
                        : $"""
                        {progressHint}

                        {convergenceStatus}

                        根据当前讨论进度，请决定下一个应该发言的人是谁？
                        未收到用户 stop 指令前不要输出 [结束]。
                        只输出一个名字，或输出 [结束]。
                        """;

                    var nextSpeakerResponse = await orchestrator.RunAsync(selectPrompt, orchestratorThread);
                    rawNextSpeaker = nextSpeakerResponse.Text?.Trim() ?? string.Empty;
                }

                // 检查主持人是否建议结束
                if (!fromUserSelection && (rawNextSpeaker.Contains("[结束]") || rawNextSpeaker.Equals("结束", StringComparison.OrdinalIgnoreCase)))
                {
                    if (interactiveMode)
                    {
                        var (stopNow, manualNext) = AskManualControl("主持人建议结束，是否需要继续？");
                        if (stopNow)
                        {
                            userRequestedStop = true;
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(manualNext))
                        {
                            pendingManualSpeaker = manualNext;
                        }
                        else
                        {
                            pendingManualSpeaker = agentOrder[round % agentOrder.Count];
                        }
                    }
                    else
                    {
                        pendingManualSpeaker = agentOrder[(round) % agentOrder.Count];
                    }

                    continue;
                }

                string nextSpeaker = rawNextSpeaker;
                int displayRound = round + 1;

                // 查找对应的 Agent
                AIAgent? currentAgent = null;
                string? agentName = null;
                bool isValidSelection = false;
                foreach (var kvp in agents)
                {
                    if (nextSpeaker.Contains(kvp.Key))
                    {
                        currentAgent = kvp.Value;
                        agentName = kvp.Key;
                        isValidSelection = true;
                        break;
                    }
                }

                if (currentAgent == null)
                {
                    // 如果主持人给出了无效的名字，按顺序选择
                    var keys = agentOrder;
                    agentName = keys[round % keys.Count];
                    currentAgent = agents[agentName];
                    isValidSelection = false;
                }

                // 显示主持人邀请了谁（主持人=绿色，用户指定=青色）
                if (isValidSelection)
                {
                    if (fromUserSelection)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine($"\n📝 用户指定：请【{agentName}】发言。");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n主持人：请【{agentName}】发言。");
                    }
                }
                else
                {
                    Console.ForegroundColor = fromUserSelection ? ConsoleColor.DarkCyan : ConsoleColor.Green;
                    string source = fromUserSelection ? "用户" : "主持人";
                    Console.WriteLine($"\n⚠️ {source}选择了无效名字「{nextSpeaker}」，自动改为【{agentName}】发言。");
                }
                Console.ResetColor();

                // 让选中的 Agent 发言（流式输出）
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n【{agentName}】(第 {displayRound} 轮):");
                Console.ResetColor();

                // 构建代理提示：投票阶段极简提示，常规阶段保留原逻辑
                ChatMessage agentPrompt;
                if (discussionProgress.IsVotingPhase && !discussionProgress.VotingCompleted)
                {
                    var allNames = discussionProgress.MentionedNames.ToList();
                    agentPrompt = new ChatMessage(
                        ChatRole.User,
                        $"""
                        已进入投票阶段。请不要进行分析或提出新名字。
                        候选清单（{allNames.Count}）：{string.Join(", ", allNames)}
                        请仅在回复末尾追加一行投票，不要添加其他内容：
                        【投票】唐XX, 唐XX, 唐XX, 唐XX, 唐XX
                        """);
                }
                else
                {
                    string contextSummary = discussionProgress.GetContextSummary();
                    string debateDirective = discussionProgress.GetDebateDirective(agentName!);
                    string nameGuidance = discussionProgress.GetNameGuidance();
                    string convergenceDirective = discussionProgress.GetConvergenceDirective(agentName!);

                    agentPrompt = new ChatMessage(
                        ChatRole.User,
                        $"""
                        {contextSummary}

                        {debateDirective}
                        {nameGuidance}
                        {convergenceDirective}

                        ---
                        请以"{agentName}"的身份发言，并遵循以下步骤：
                        1. 若处于常规阶段：可以对已经提出的名字先提出一个质疑点，再补充论据，没有提出过名字则不需要质疑；
                        2. 若处于常规阶段：可提出 1-3 个新名字或优化已有名字；
                        3. 若处于投票阶段：请直接在末尾追加一行【投票】并列出5个最喜欢的候选；
                        4. 最后一行务必保持票据格式正确，以便统计。
                        """);
                }
                discussionHistory.Add(agentPrompt);
                if (!debateHistoryFrozen) debateHistory.Add(agentPrompt);
                List<ChatMessage> agentMessages;
                if (discussionProgress.IsVotingPhase && !discussionProgress.VotingCompleted)
                {
                    var votingIsolationSystem = new ChatMessage(ChatRole.System,
                        "投票阶段说明：请独立完成投票，不参考他人已投票内容或主持人总结中的票型信息。只基于候选清单做出你的5票选择，并在末尾添加【投票】行。");
                    agentMessages = new List<ChatMessage>(debateHistory);
                    agentMessages.Add(votingIsolationSystem);
                    agentMessages.Add(agentPrompt);
                }
                else
                {
                    agentMessages = new List<ChatMessage>(discussionHistory);
                }
                string agentResponse = await RunAgentWithRetry(currentAgent, agentMessages, agentName!, displayRound, discussionProgress);

                // 主持人总结刚才的发言（使用独立的 Thread，流式输出，颜色与主持人一致）
                string summaryHint = string.Empty;
                if (discussionProgress.IsVotingPhase)
                {
                    summaryHint = discussionProgress.VotingCompleted
                        ? "投票已完成，请准备公布票型。"
                        : discussionProgress.GetConvergenceStatus();
                }

                string summaryPrompt = $"""
                    刚才【{agentName}】发言完毕。

                    请用1-3句话总结要点，并复述上一位建议的名字，同时点明关键结论和下一步关注点。
                    只做总结，不要邀请下一位。
                    {summaryHint}
                    """;

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green; // 主持人用绿色
                Console.Write("主持人总结：");
                var sbSummary = new StringBuilder();
                await foreach (var update in orchestrator.RunStreamingAsync(summaryPrompt, orchestratorThread))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        Console.Write(update.Text);
                        sbSummary.Append(update.Text);
                    }
                }
                Console.ResetColor();
                var summaryText = sbSummary.ToString();

                // 将总结也记录到讨论历史
                discussionHistory.Add(new ChatMessage(ChatRole.Assistant, $"【主持人总结】{summaryText}"));
                if (!debateHistoryFrozen) debateHistory.Add(new ChatMessage(ChatRole.Assistant, $"【主持人总结】{summaryText}"));
                discussionProgress.RecordSummary(summaryText);

                Console.WriteLine();
                Console.WriteLine(new string('-', 50));

                if (discussionProgress.IsVotingPhase && discussionProgress.VotingCompleted)
                {
                    var results = discussionProgress.GetVoteResults();
                    string resultsText = string.Join("; ", results.Select(r => $"{r.Name}: {r.Votes}票"));
                    var closingPrompt = $"""
                        全部投票已完成。
                        请以主持人身份公布票型（从高到低）、指出得票前列的名字，并给出后续建议（如试写、亲友反馈、小范围A/B测试）。
                        可按如下票型数据组织口径：
                        {resultsText}
                        然后正式宣布会议结束并送上祝福。
                        """;

                    Console.ForegroundColor = ConsoleColor.Green; // 主持人用绿色
                    Console.Write("主持人终结：");
                    var sbClosing = new StringBuilder();
                    await foreach (var update in orchestrator.RunStreamingAsync(closingPrompt, orchestratorThread))
                    {
                        if (!string.IsNullOrEmpty(update.Text))
                        {
                            Console.Write(update.Text);
                            sbClosing.Append(update.Text);
                        }
                    }
                    Console.ResetColor();
                    string closingText = sbClosing.ToString();
                    discussionHistory.Add(new ChatMessage(ChatRole.Assistant, $"【主持人终结】{closingText}"));
                    break;
                }

                // 手动控制下一轮
                if (interactiveMode)
                {
                    var (stopAfterSummary, manualAfterSummary) = AskManualControl("如需手动控制下一轮，请输入参与者名字；输入 stop 结束；直接回车交给主持人。");
                    if (stopAfterSummary)
                    {
                        userRequestedStop = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(manualAfterSummary))
                    {
                        pendingManualSpeaker = manualAfterSummary;
                    }
                }

                round++;
            }

            // 输出最终统计
            Console.WriteLine("\n=== 讨论统计 ===");
            Console.WriteLine(discussionProgress.GetFinalStats());
            Console.WriteLine("\n=== 讨论记录结束 ===");
        }

        private static (bool stop, string? nextSpeaker) AskManualControl(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"\n{prompt}");
            Console.WriteLine("输入 stop 结束讨论，输入参与者名字可强制下一位发言，直接回车则保持自动模式。");
            Console.ResetColor();

            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return (false, null);
            }

            string trimmed = input.Trim();
            if (trimmed.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                return (true, null);
            }

            return (false, trimmed);
        }

        /// <summary>
        /// 执行代理发言，并在投票阶段强制其提交【投票】行；若多次失败，自动选取其最近提过的名字做票据补全。
        /// </summary>
        private static async Task<string> RunAgentWithRetry(AIAgent agent, List<ChatMessage> messages, string agentName, int displayRound, DiscussionProgress progress)
        {
            int maxRetries = progress.IsVotingPhase ? 2 : 0;
            int attempt = 0;
            string lastResponse = string.Empty;

            while (true)
            {
                StringBuilder sb = new();
                await foreach (var update in agent.RunStreamingAsync(messages))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        Console.Write(update.Text);
                        sb.Append(update.Text);
                    }
                }
                lastResponse = sb.ToString();

                // 投票阶段校验
                if (progress.IsVotingPhase && !progress.HasAgentVoted(agentName))
                {
                    bool hasVoteLine = lastResponse.Contains("【投票】");
                    if (!hasVoteLine && attempt < maxRetries)
                    {
                        attempt++;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write($"\n(提示: 第{attempt}次提醒，{agentName}尚未提供投票行，自动补充指导继续重试...)\n");
                        Console.ResetColor();
                        // 在原消息后添加一个投票提醒，再次尝试
                        messages.Add(new ChatMessage(ChatRole.User, "请在回复末尾添加：【投票】唐XX, 唐XX, 唐XX, 唐XX, 唐XX"));
                        continue;
                    }
                    else if (!hasVoteLine && attempt >= maxRetries)
                    {
                        // 自动生成票据：选前5个出现过的名字（简单策略）
                        var autoNames = progress.MentionedNames.Take(5).ToList();
                        string autoLine = $"【投票】{string.Join(", ", autoNames)}";
                        lastResponse += "\n" + autoLine + "\n(系统自动补全投票)";
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"已为 {agentName} 自动补全投票：{autoLine}");
                        Console.ResetColor();
                    }
                }

                // 记录发言
                messages.Add(new ChatMessage(ChatRole.Assistant,
                    $"【{agentName}发言】(第 {displayRound} 轮)\n{lastResponse}"));
                progress.RecordSpeech(agentName, lastResponse);
                return lastResponse;
            }
        }
    }
}