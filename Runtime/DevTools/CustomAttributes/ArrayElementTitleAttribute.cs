using UnityEngine;

namespace DavidUtils.Editor.DevTools.CustomAttributes
{
    /// <summary>
    /// ATRIBUTO para asignar un nombre a cada elemento de un array
    /// En el inspector se verá el nombre de cada elemento en lugar de su índice
    ///
    /// Ejemplo:
    /// 
    /// struct A : IArrayElementTitle {
    /// 
    ///      [HideInInspector] public Type type;
    ///      public string Name => type.ToString();
    ///      ...
    /// }
    /// 
    /// A[] array;
    ///
    /// </summary>
    public class ArrayElementTitleAttribute : PropertyAttribute
    {
        public string VarName { get; }
     
        public ArrayElementTitleAttribute(string elementTitleVar = "")
        {
          VarName = elementTitleVar;
        }
        
        public interface IArrayElementTitle
        {
            public string Name
            {
                get;
            }
        }
    }
}
