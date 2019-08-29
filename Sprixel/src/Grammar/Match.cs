using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TriAxis.RunSharp;

namespace Sprixel {
    public sealed class Match : PrototypeChain<string, Match> {

        public int Start;
        public int End;
        public bool Success;
        public Operand Result;
        public Match Next;
        public string Name;

        public Match()
            : base() {
                Name = "";
        }

        public Match(Match parent, int start, int end) : base(parent) {
            Start = start;
            End = end;
        }
        /*
        public Match this[string key, int flag1] {
            get {
                Match last;
                try {
                    last = this[key];
                } catch (Exception e) {
                    return null;
                }
                if (last == null || !last.Success)
                    return null;
                while (last.Next != null && last.Next.Success)
                    last = last.Next;
                return last;
            }
            set {
                Match last;
                try {
                    last = this[key, 1];
                } catch (Exception e) {
                    return;
                }
                if (last == null) {
                    if (Parent != null) {
                        Parent[key, true] = value;
                    } else {
                        this[key, true] = value;
                    }
                    return;
                }   
                while (last.Next != null && last.Next.Success)
                    last = last.Next;
                last.Next = value;
            }
        }

        public Match this[string key, int flag1, int flag2] {
            get {
                try {
                    return base[key, true];
                } catch (Exception e) {
                    return this[key, 1];
                }
            }
        }

        public Match this[string key, int flag1, int flag2, int flag3] {
            get {
                try {
                    return base[key, true];
                } catch (Exception e) {
                    try {
                        return this[key, 1];
                    } catch (Exception f) {
                        return this[key];
                    }
                }
            }
        }

        public void Pop(string key) {
            var last = this[key];
            Match second2last = null;
            if (last == null || !last.Success) {
                return;
            }
            while (last.Next != null && last.Next.Success) {
                second2last = last;
                last = last.Next;
            }
            if (second2last != null && second2last.Success) {
                second2last.Next = null;
            }
        }

        public void PopNext(string key, Match m) {
            Pop(key);
            var last = this[key];
            Match second2last = null;
            if (last == null || !last.Success) {
                this[key, true] = m;
                return;
            }
            while (last.Next != null && last.Next.Success) {
                second2last = last;
                last = last.Next;
            }
            if (second2last != null && second2last.Success) {
                second2last.Next = m;
            } else {
                this.Parent[key, true] = m;
            }
        }
        */
        public Match(string name, Match parent, int offset)
            : base(parent) {
            Start = offset;
            Name = name;
            if (parent.HasOwnKey(name)) {
            //    while (existing.Next != null && existing.Next.Success)
            //        existing = existing.Next;
            //    existing.Next = this;
                parent[name] = this;
            } else if (parent[name] != null) {
                parent[name, true] = this;
            } else {
                parent[name, true, true] = this;
            }
        }
        
        public Match MParent(int offset) {
            Success = true;
            End = offset;
            return (Match)Parent;
        }

        public Match MParent() {
            return (Match)Parent;
        }
        
        public Match[] Matches(string name) {
            List<Match> matches = new List<Match>();
            Match last;
            if (TryGetValue(name, out last) && last.Success) {
                matches.Add(last);
                while (last.Next != null && last.Next.Success) {
                    matches.Add(last);
                    last = last.Next;
                }
            }
            return matches.ToArray();
        }

        public string Matches(UTF32String input) {
            var sb = new StringBuilder();
            Keys.ToList().ForEach((string s) => {
                foreach (var mi in Matches(s)) {
                    sb.Append(s + ": " + input.ToString().Substring(mi.Start, mi.End - mi.Start) + "\n");
                }
            });
            return sb.ToString();
        }

        public string SubStringOf(UTF32String input) {
            return input.Match(this);
        }
    }
}
