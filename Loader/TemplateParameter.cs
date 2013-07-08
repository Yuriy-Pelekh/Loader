using System.Collections.Generic;

namespace Loader
{
  public static class TemplateParameter
  {
    public static readonly string TemplateKey = "Template";

    public static string TemplateName { get; private set; }

    public static void GetTemaplate(IDictionary<string, string> parameters)
    {
      if (parameters.ContainsKey(TemplateKey))
      {
        TemplateName = parameters[TemplateKey];
      }
    }
  }
}
