using System.Collections.Generic;
using System.Windows.Controls;
using Mineguide.perspectives;
using pm4h.windows.ui;
using pm4h.windows.utils;
using pmapp.data.utils;

namespace Mineguide
{
    [PluginConfig]
    public class MineguidePluginConfig : PMAppPluginConfig
    {
        public override void Init()
        {
            StyleHelper.LoadLocalResourceDictionary("styles/MGIcons.xaml");
        }



        public override IEnumerable<Button> getExperimentTools(MainPerspectiveArgs args)
        {
            var res = new List<Button>();

            res.Add(CreateActionButton("MineGuider", () =>
            {
                PMAppWinHelper.CreateNewPerspective(args.ContextId, "MineGuider", "MineGuider", new MineguideEditor(args));
            }, "pm4h.Resources.IconPath.Mineguide.Editor", "Open MineGuider tool"));

            return res;
        }
    }
}
