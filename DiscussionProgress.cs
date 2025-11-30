using System;
using System.Collections.Generic;
using System.Text;

namespace LLM_NameDiscussion
{
    internal class DiscussionProgress
    {
        private const int ConvergenceThreshold = 30;
        private const int FinalListTarget = 5;

        private readonly HashSet<string> _eligibleVoters = new(StringComparer.Ordinal);

        private readonly List<string> _finalizedNames = new();

        private readonly List<string> _keyPoints = new();

        private readonly Dictionary<string, string> _latestStatements = new();

        // --- 名字统计 ---
        private readonly HashSet<string> _mentionedNames = new();

        private readonly Dictionary<string, int> _nameFrequency = new(StringComparer.Ordinal);

        private readonly List<string> _nameTimeline = new();

        private readonly Dictionary<string, int> _shortlistVotes = new(StringComparer.Ordinal);

        // --- 发言追踪 ---
        private readonly Dictionary<string, int> _speakCounts = new();

        private readonly List<string> _speakOrder = new();
        private readonly HashSet<string> _votedAgents = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _voteTally = new(StringComparer.Ordinal);
        private int _currentPhase = 1;
        private string? _lastSpeaker;
        private string? _lastStatement;
        private bool _votingTriggered;      // 达到阈值后进入投票阶段

        /// <summary>
        /// 最终确认的 5 个名字（若已定稿）。
        /// </summary>
        public IReadOnlyList<string> FinalizedNames => _finalizedNames;

        /// <summary>
        /// 是否已经确认 5 个最终名字。（投票模式下不使用，保留兼容）
        /// </summary>
        public bool FinalListConfirmed => _finalizedNames.Count >= FinalListTarget;

        /// <summary>
        /// 兼容旧名：当累计候选数量达到阈值时进入投票阶段。
        /// </summary>
        public bool IsConvergencePhase
        {
            get
            {
                return IsVotingPhase;
            }
        }

        /// <summary>
        /// 是否进入投票阶段。
        /// </summary>
        public bool IsVotingPhase => _votingTriggered;

        /// <summary>
        /// 指示是否开启用户手动干预提示；仅用于提示信息。
        /// </summary>
        public bool ManualControlEnabled { get; set; }

        /// <summary>
        /// 所有出现过的候选名字集合。
        /// </summary>
        public IReadOnlyCollection<string> MentionedNames => _mentionedNames;

        /// <summary>
        /// 是否所有登记选民都已投票。
        /// </summary>
        public bool VotingCompleted => _votingTriggered && _eligibleVoters.Count > 0 && _votedAgents.Count >= _eligibleVoters.Count;

        /// <summary>
        /// 若尚未登记选民，则用提供的列表进行登记（幂等）。
        /// </summary>
        public void EnsureEligibleVoters(IEnumerable<string> voters)
        {
            if (_eligibleVoters.Count > 0)
                return;
            foreach (var v in voters)
                _eligibleVoters.Add(v);
        }

        /// <summary>
        /// 汇总最近的主持人要点、最新候选以及收敛状态，供专家快速了解上下文。
        /// </summary>
        public string GetContextSummary()
        {
            if (_keyPoints.Count == 0)
                return "这是讨论的开始。";

            var sb = new StringBuilder();
            var recentPoints = _keyPoints.TakeLast(3).ToList();
            sb.AppendLine("## 近期讨论要点");
            sb.AppendLine(string.Join("\n", recentPoints.Select((p, i) => $"{i + 1}. {p}")));

            if (_nameTimeline.Count > 0)
            {
                var latest = _nameTimeline.TakeLast(5);
                sb.AppendLine($"\n最新被提到的名字：{string.Join("、", latest)}");
            }

            if (IsVotingPhase)
            {
                sb.AppendLine($"投票进度：{_votedAgents.Count}/{_eligibleVoters.Count}");
                var leaders = GetVoteLeaders(5).ToList();
                if (leaders.Count > 0)
                    sb.AppendLine($"当前领先：{string.Join("、", leaders)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 面向专家的收敛指令，要求他们提交【收敛名单】或确认【最终名单】。
        /// </summary>
        public string GetConvergenceDirective(string currentSpeaker)
        {
            if (!IsVotingPhase)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("## 投票阶段指令");
            sb.AppendLine($"已累计 {_mentionedNames.Count} 个候选，请从中投出你最喜欢的 5 个名字。");
            sb.AppendLine("请严格按以下格式在回答末尾新增一行：");
            sb.AppendLine("【投票】唐XX, 唐XX, 唐XX, 唐XX, 唐XX");
            sb.AppendLine("注意：只能投现有候选；超过5个仅计前5个；重复不计。");

            return sb.ToString();
        }

        /// <summary>
        /// 返回给主持人的收敛状态描述，用于决定下一位发言者。
        /// </summary>
        public string GetConvergenceStatus()
        {
            if (!IsVotingPhase)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"候选总数：{_mentionedNames.Count} (阈值 {ConvergenceThreshold})");
            sb.AppendLine($"投票进度：{_votedAgents.Count}/{_eligibleVoters.Count}");

            var leaders = GetVoteLeaders(5).ToList();
            if (leaders.Count > 0)
                sb.AppendLine($"当前领先：{string.Join(", ", leaders)}");
            else
                sb.AppendLine("尚未产生有效投票。");

            return sb.ToString();
        }

        /// <summary>
        /// 返回当前说话者需要围绕的质疑焦点及热点名字提醒。
        /// </summary>
        public string GetDebateDirective(string currentSpeaker)
        {
            if (!string.IsNullOrWhiteSpace(_lastSpeaker) && !string.Equals(_lastSpeaker, currentSpeaker, StringComparison.Ordinal))
            {
                string snippet = ToSnippet(_lastStatement);
                return $"上一位【{_lastSpeaker}】的观点摘录：{snippet}\n请先针对该观点提出质疑或追问，再给出你的判断。";
            }

            if (_latestStatements.Count > 0)
            {
                var other = _latestStatements.FirstOrDefault(kvp => !string.Equals(kvp.Key, currentSpeaker, StringComparison.Ordinal));
                if (string.IsNullOrEmpty(other.Key))
                {
                    other = _latestStatements.First();
                }
                string snippet = ToSnippet(other.Value);
                if (IsVotingPhase)
                {
                    return "已进入投票阶段，请直接根据候选名单进行投票，不再展开分析。";
                }

                return $"可参考其他专家观点（示例：{snippet}），请挑选其一提出质疑。";
            }

            return "请主动提出至少一个质疑点，避免直接给出答案。";
        }

        /// <summary>
        /// 输出讨论结束时的统计摘要。
        /// </summary>
        public string GetFinalStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"总发言轮数：{_speakOrder.Count}");
            sb.AppendLine($"讨论阶段：{_currentPhase}");
            sb.AppendLine("\n各参与者发言次数：");
            foreach (var kvp in _speakCounts.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  - {kvp.Key}: {kvp.Value} 次");
            }
            if (_mentionedNames.Count > 0)
            {
                sb.AppendLine($"\n提到的候选名字 ({_mentionedNames.Count} 个)：");
                sb.AppendLine($"  {string.Join(", ", _nameTimeline.TakeLast(20))}");
                if (_mentionedNames.Count > 20)
                    sb.AppendLine("  ...");

                var hotList = GetTopCandidates(5).ToList();
                if (hotList.Count > 0)
                    sb.AppendLine($"  热度前列：{string.Join(", ", hotList)}");
            }

            if (_voteTally.Count > 0)
            {
                sb.AppendLine("\n投票统计（当前）：");
                foreach (var kvp in _voteTally.OrderByDescending(x => x.Value).ThenBy(x => GetTimelineIndex(x.Key)).Take(10))
                {
                    sb.AppendLine($"  - {kvp.Key}: {kvp.Value} 票");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 指导专家应该继续扩充候选还是进入收敛，并提供最近名字样本。
        /// </summary>
        public string GetNameGuidance()
        {
            if (_mentionedNames.Count == 0)
                return "目前还没有明确候选名字，请务必提出全新的 1-2 个名字。";

            var latest = _nameTimeline.TakeLast(6);
            var latestText = string.Join(", ", latest);

            if (IsVotingPhase)
                return $"已进入投票阶段，最近出现的名字包括：{latestText}。请等待主持人提供完整候选清单后投票。";

            return $"当前候选（部分）：{latestText}。可以继续提出 1-2 个全新名字，也可优化已有名字并说明理由。";
        }

        /// <summary>
        /// 获取下一位尚未投票的参与者（按给定顺序）。
        /// </summary>
        public string? GetNextUnvoted(IEnumerable<string> orderedAgents)
        {
            foreach (var a in orderedAgents)
            {
                if (_eligibleVoters.Contains(a) && !_votedAgents.Contains(a))
                    return a;
            }
            return null;
        }

        /// <summary>
        /// 返回给主持人的阶段提示，包括尚未发言者、候选统计等。
        /// </summary>
        public string GetProgressHint()
        {
            string phase = _currentPhase switch
            {
                1 => "开场阶段（需要爸爸先介绍背景）",
                2 => "品牌/语言/科技碰撞阶段",
                3 => "心理与文化深化阶段",
                4 => "数据与国际收官阶段",
                5 => "投票阶段：每人选择5个心仪名字",
                6 => "结果公布阶段",
                _ => "讨论进行中"
            };

            var notSpoken = new[] { "爸爸", "品牌策略师", "语言学家", "科技创业导师", "教育心理顾问", "文化史策展人", "数据洞察分析师", "国际传播顾问" }
                .Where(name => !_speakCounts.ContainsKey(name) || _speakCounts[name] == 0)
                .ToList();

            string hint = $"当前阶段：{phase}\n已发言 {_speakOrder.Count} 次";
            if (notSpoken.Count > 0 && notSpoken.Count < 8)
                hint += $"\n尚未发言：{string.Join("、", notSpoken)}";

            if (_mentionedNames.Count > 0)
                hint += $"\n已提到的候选名字数：{_mentionedNames.Count}";

            if (IsVotingPhase)
            {
                hint += $"\n投票进度：{_votedAgents.Count}/{_eligibleVoters.Count}";
                var leaders = GetVoteLeaders(5).ToList();
                if (leaders.Count > 0)
                    hint += $"\n当前领先：{string.Join("、", leaders)}";
            }

            if (ManualControlEnabled)
                hint += "\n（提示：输入 stop 可结束，输入名字可指定下一位。）";

            return hint;
        }

        /// <summary>
        /// 根据【收敛名单】票数输出当前呼声最高的候选。
        /// </summary>
        public IEnumerable<string> GetShortlistLeaders(int count = 5)
        {
            return _shortlistVotes
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => GetTimelineIndex(kvp.Key))
                .Take(count)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// 根据出现频次与时间顺序获取最热门的候选名字。
        /// </summary>
        public IEnumerable<string> GetTopCandidates(int count = 10)
        {
            return _nameFrequency
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => GetTimelineIndex(kvp.Key))
                .Take(count)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// 返回当前票数领先的名字列表。
        /// </summary>
        public IEnumerable<string> GetVoteLeaders(int count)
        {
            return _voteTally
                .OrderByDescending(x => x.Value)
                .ThenBy(x => GetTimelineIndex(x.Key))
                .Take(count)
                .Select(x => x.Key);
        }

        /// <summary>
        /// 返回完整票型（降序）。
        /// </summary>
        public IList<(string Name, int Votes)> GetVoteResults()
        {
            return _voteTally
                .OrderByDescending(x => x.Value)
                .ThenBy(x => GetTimelineIndex(x.Key))
                .Select(x => (x.Key, x.Value))
                .ToList();
        }

        /// <summary>
        /// 查询某个参与者是否已成功提交投票。
        /// </summary>
        public bool HasAgentVoted(string agentName) => _votedAgents.Contains(agentName);

        /// <summary>
        /// 记录一次发言，包含发言顺序、内容摘要以及由内容推导出的候选名单。
        /// </summary>
        public void RecordSpeech(string speaker, string content)
        {
            LogSpeechMeta(speaker, content);
            TrackNamesAndShortlistAndVotes(speaker, content);
            UpdateConvergenceState();
            UpdatePhase();
        }

        /// <summary>
        /// 保存主持人总结，后续可用于上下文提示。
        /// </summary>
        public void RecordSummary(string summary)
        {
            if (!string.IsNullOrWhiteSpace(summary))
                _keyPoints.Add(summary);
        }

        /// <summary>
        /// 开始投票阶段并设置合格投票人（通常为所有参与者）。
        /// </summary>
        public void StartVotingPhase(IEnumerable<string> voters)
        {
            _votingTriggered = true;
            _eligibleVoters.Clear();
            foreach (var v in voters)
                _eligibleVoters.Add(v);
        }

        /// <summary>
        /// 判断一个字符是否可作为名字的组成（简单过滤空白和标点）。
        /// </summary>
        private static bool IsNameChar(char c)
        {
            if (char.IsWhiteSpace(c))
                return false;

            return char.IsLetter(c);
        }

        /// <summary>
        /// 判断一个字符串是否符合“唐”字开头、2-4 字母的候选格式。
        /// </summary>
        private static bool LooksLikeCandidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string trimmed = name.Trim().Trim('\u3002', '\uFF0C', ',', '\u3001', '\uFF01', '!', '?', '\uFF1F', ';', '\uFF1B');
            if (!trimmed.StartsWith('唐'))
                return false;

            int length = trimmed.Count(char.IsLetter);
            return length is >= 2 and <= 4;
        }

        /// <summary>
        /// 解析形如【marker】唐若岚的片段，返回名字列表。
        /// </summary>
        private static List<string> ParseBracketedNames(string content, string marker)
        {
            List<string> results = new();
            if (string.IsNullOrWhiteSpace(content))
                return results;

            string flag = $"【{marker}】";
            int searchIndex = 0;
            while (true)
            {
                int start = content.IndexOf(flag, searchIndex, StringComparison.Ordinal);
                if (start < 0)
                    break;

                start += flag.Length;
                int end = content.IndexOf('【', start);
                int newline = content.IndexOf('\n', start);
                if (newline >= 0 && (end < 0 || newline < end))
                    end = newline;
                if (end < 0)
                    end = content.Length;

                string segment = content[start..end];
                foreach (var candidate in SplitNamesSegment(segment))
                {
                    if (LooksLikeCandidateName(candidate))
                        results.Add(candidate.Trim());
                }

                searchIndex = end;
            }

            return results;
        }

        /// <summary>
        /// 使用常见分隔符切分候选名字段落。
        /// </summary>
        private static IEnumerable<string> SplitNamesSegment(string segment)
        {
            return segment
                .Split(new[] { '\u3001', ',', '\uFF0C', '|', '/', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());
        }

        /// <summary>
        /// 截取一段文本用于在提示中引用，避免过长。
        /// </summary>
        private static string ToSnippet(string? content, int maxLength = 180)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "（暂无内容）";

            string normalized = content.Replace("\r", " ").Replace("\n", " ").Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "…";
        }

        /// <summary>
        /// 从文本中提取以“唐”开头的 2-4 字候选名字，并记录出现顺序与频率。
        /// </summary>
        private void ExtractNames(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] != '唐')
                    continue;

                var candidate = new StringBuilder();
                candidate.Append('唐');

                int j = i + 1;
                while (j < content.Length && IsNameChar(content[j]) && candidate.Length < 3)
                {
                    candidate.Append(content[j]);
                    j++;
                }

                if (candidate.Length < 2)
                    continue;

                string name = candidate.ToString();
                if (_mentionedNames.Add(name))
                    _nameTimeline.Add(name);

                if (!_nameFrequency.ContainsKey(name))
                    _nameFrequency[name] = 0;
                _nameFrequency[name]++;
            }
        }

        /// <summary>
        /// 解析【投票】行，统计每位代理最多5票，仅计首次有效投票，且票必须来自候选集合。
        /// </summary>
        private void ExtractVotes(string speaker, string content)
        {
            if (!_votingTriggered || string.IsNullOrWhiteSpace(content))
                return;

            if (_votedAgents.Contains(speaker))
                return; // 已投过票

            var votes = ParseBracketedNames(content, "投票");
            if (votes.Count == 0)
                return;

            // 过滤非法候选，并限制最多5票
            var validVotes = votes
                .Where(v => _mentionedNames.Contains(v))
                .Distinct()
                .Take(5)
                .ToList();

            if (validVotes.Count == 0)
                return;

            foreach (var name in validVotes)
            {
                if (!_voteTally.ContainsKey(name))
                    _voteTally[name] = 0;
                _voteTally[name]++;
            }

            _votedAgents.Add(speaker);
        }

        /// <summary>
        /// 返回名字首次出现的位置，用于保持时间顺序。
        /// </summary>
        private int GetTimelineIndex(string name)
        {
            int index = _nameTimeline.IndexOf(name);
            return index >= 0 ? index : int.MaxValue;
        }

        /// <summary>
        /// 写入发言频次、顺序以及最近一次观点摘要。
        /// </summary>
        private void LogSpeechMeta(string speaker, string content)
        {
            if (!_speakCounts.ContainsKey(speaker))
                _speakCounts[speaker] = 0;
            _speakCounts[speaker]++;

            _speakOrder.Add(speaker);
            _lastSpeaker = speaker;
            _lastStatement = content;
            _latestStatements[speaker] = content;
        }

        /// <summary>
        /// 解析发言内容中的候选名字、收敛名单与最终名单信号。
        /// </summary>
        private void TrackNamesAndShortlistAndVotes(string speaker, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            ExtractNames(content);
            ExtractVotes(speaker, content);
        }

        /// <summary>
        /// 当候选数量达到阈值后标记收敛阶段。
        /// </summary>
        private void UpdateConvergenceState()
        {
            if (_votingTriggered)
                return;

            if (_mentionedNames.Count >= ConvergenceThreshold)
                _votingTriggered = true;
        }

        /// <summary>
        /// 根据当前讨论轮次、收敛状态和最终名单情况同步阶段。
        /// </summary>
        private void UpdatePhase()
        {
            if (VotingCompleted)
            {
                _currentPhase = 6; // 公布结果
                return;
            }

            if (IsVotingPhase)
            {
                _currentPhase = 5; // 投票中
                return;
            }

            int totalSpeeches = _speakOrder.Count;
            if (totalSpeeches <= 1)
                _currentPhase = 1;
            else if (totalSpeeches <= 5)
                _currentPhase = 2;
            else if (totalSpeeches <= 8)
                _currentPhase = 3;
            else
                _currentPhase = 4;
        }
    }
}