namespace LuC.Tree
{
    public class GlobalDeclarationStatement : Member
    {
        public Field Global { get; }

        public GlobalDeclarationStatement(int start, int length, Field global) : base(start, length, global.Name)
        {
            Global = global;

            Global.SetParent(this);
        }
    }
}
