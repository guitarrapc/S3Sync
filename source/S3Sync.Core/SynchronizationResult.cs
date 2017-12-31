using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace S3Sync.Core
{
    public class SynchronizationResult
    {
        public int TotalCount { get { return New + Update + Skip + Remove; } }
        public int New { get; set; }
        public int Update { get; set; }
        public int Skip { get; set; }
        public int Remove { get; set; }

        private int GetMax(string left, int right)
        {
            return Math.Max(left.Length, right.ToString().Length);
        }

        private string PaddingTitle(int max, int text)
        {
            return PaddingTitle(max, text.ToString());
        }

        private string PaddingTitle(int max, string text)
        {
            return string.Format($"{{0, {max}}}", text);
        }

        /// <summary>
        /// Markdown Friendly Table format
        /// </summary>
        /// <returns></returns>
        public string ToMarkdown()
        {
            var totalMax = GetMax(nameof(TotalCount), TotalCount);
            var totalSeparator = new string('-', totalMax);
            var totalTitle = PaddingTitle(totalMax, nameof(TotalCount));
            var totalValue = PaddingTitle(totalMax, TotalCount);

            var newMax = GetMax(nameof(New), New);
            var newSeparator = new string('-', newMax);
            var newTitle = PaddingTitle(newMax, nameof(New));
            var newValue = PaddingTitle(newMax, New);

            var updateMax = GetMax(nameof(Update), Update);
            var updateSeparator = new string('-', updateMax);
            var updateTitle = PaddingTitle(updateMax, nameof(Update));
            var updateValue = PaddingTitle(updateMax, Update);

            var skipMax = GetMax(nameof(Skip), Skip);
            var skipSeparator = new string('-', skipMax);
            var skipTitle = PaddingTitle(skipMax, nameof(Skip));
            var skipValue = PaddingTitle(skipMax, Skip);

            var removeMax = GetMax(nameof(Remove), Remove);
            var removeSeparator = new string('-', removeMax);
            var removeTitle = PaddingTitle(removeMax, nameof(Remove));
            var removeValue = PaddingTitle(removeMax, Remove);

            return $@"| {totalTitle}  | {newTitle}  | {updateTitle}  | {skipTitle}  | {removeTitle} |
| {totalSeparator}: | {newSeparator}: | {updateSeparator}: | {skipSeparator}: | {removeSeparator}:|
| {totalValue}  | {newValue}  | {updateValue}  | {skipValue}  | {removeValue} |";
        }
    }
}
