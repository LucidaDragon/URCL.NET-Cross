namespace LuC.Tree
{
    public abstract class TreeObject
    {
        public int Start { get; }
        public int Length { get; }
        public TreeObject Parent { get; private set; }

        public TreeObject(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public void SetParent(TreeObject parent)
        {
            Parent = parent;
        }

        public string GetSource(string src)
        {
            return src.Substring(Start, Length);
        }

        public Namespace GetNamespace(bool throwException = true)
        {
            if (this is Namespace ns)
            {
                return ns;
            }
            else if (Parent is null)
            {
                if (throwException) throw new SourceError(this, SourceError.NamespaceNotResolved);
                return null;
            }
            else
            {
                return Parent.GetNamespace();
            }
        }

        public Function GetFunction(bool throwException = true)
        {
            if (this is Function f)
            {
                return f;
            }
            else if (Parent is null)
            {
                if (throwException) throw new SourceError(this, SourceError.FunctionNotResolved);
                return null;
            }
            else
            {
                return Parent.GetFunction();
            }
        }

        public Statement GetStatement(bool throwException = true)
        {
            if (this is Statement s)
            {
                return s;
            }
            else if (Parent is null)
            {
                if (throwException) throw new SourceError(this, SourceError.StatementNotResolved);
                return null;
            }
            else
            {
                return Parent.GetStatement();
            }
        }
    }
}
