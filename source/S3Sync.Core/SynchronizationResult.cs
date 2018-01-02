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
        public bool DryRun { get; set; }

        /// <summary>
        /// Markdown Friendly Table format
        /// </summary>
        /// <returns></returns>
        public string ToMarkdown()
        {
            var totalItem = new MarkDownTabkeItem(TotalCount, nameof(TotalCount));
            var newItem = new MarkDownTabkeItem(New, nameof(New));
            var updateItem = new MarkDownTabkeItem(Update, nameof(Update));
            var skipItem = new MarkDownTabkeItem(Skip, nameof(Skip));
            var removeItem = new MarkDownTabkeItem(Remove, nameof(Remove));
            var isDryItem = new MarkDownTabkeItem(DryRun, nameof(DryRun));

            return $@"| {totalItem.Title}  | {newItem.Title}  | {updateItem.Title}  | {skipItem.Title}  | {removeItem.Title}  | {isDryItem.Title}  |
| {totalItem.Separator}: | {newItem.Separator}: | {updateItem.Separator}: | {skipItem.Separator}: | {removeItem.Separator}: | {isDryItem.Separator}: |
| {totalItem.Value}  | {newItem.Value}  | {updateItem.Value}  | {skipItem.Value}  | {removeItem.Value}  | {isDryItem.Value}  |";
        }

        private class MarkDownTabkeItem
        {
            public string Separator { get; set; }
            public string Title { get; set; }
            public string Value { get; set; }

            public MarkDownTabkeItem(int count, string name)
            {
                var max = Math.Max(name.Length, count.ToString().Length);
                Separator = new string('-', max);
                Title = PaddingTitle(max, name);
                Value = PaddingTitle(max, count.ToString());
            }

            public MarkDownTabkeItem(bool state, string name)
            {
                var max = Math.Max(name.Length, state.ToString().Length);
                Separator = new string('-', max);
                Title = PaddingTitle(max, name);
                Value = PaddingTitle(max, state.ToString());
            }

            private int GetMax(string left, int right)
            {
                return Math.Max(left.Length, right.ToString().Length);
            }

            private string PaddingTitle(int max, string text)
            {
                return string.Format($"{{0, {max}}}", text);
            }
        }
    }
}
