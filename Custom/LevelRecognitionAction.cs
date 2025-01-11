using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime;
using System.Text;

namespace MFAWPF.Custom;

public class LevelRecognitionAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(LevelRecognitionAction);


    public bool Run(in IMaaContext icontext, in RunArgs iargs)
    {
        var context = icontext;
        var args = iargs;
        int i = 0, res;
        var level = () =>
        {
            if (i++ == 0)
            {
                res = Find(context, 280, 120, 220, 460);
            }
            else
            {
                res = Find(context, 480, 90, 485, 520);
            }
            IMaaImageBuffer image = new MaaImageBuffer();
            RecognitionDetail? detail;
            switch (res)
            {
                case 0:
                    Thread.Sleep(700);
                    var enteringEncouter = () =>
                    {
                        context.GetImage(ref image);
                        if (context.TemplateMatch("Sarkaz@Roguelike@StageEncounterEnter.png", image, out detail, 0.8, 1035, 453, 240, 178))
                        {

                            int x = detail.HitBox.X + 100, y = detail.HitBox.Y + 50;
                            LoggerService.LogInfo($"{x}, {y}");
                            context.Click(x, y);
                            return true;
                        }
                        return false;
                    };
                    enteringEncouter.Until();
                    MeetByChance(context, args.TaskName);
                    break;
                case 1:
                    if (!Combat(context, args.TaskName))
                        return true;
                    break;
                case 2:
                    Thread.Sleep(700);
                    var enteringTrader = () =>
                    {
                        context.GetImage(ref image);
                        if (context.TemplateMatch("Sarkaz@Roguelike@StageTraderEnter.png", image, out detail, 0.8, 1035, 453, 240, 178))
                        {
                            context.Click(detail.HitBox.X, detail.HitBox.Y);
                            return true;
                        }
                        return false;
                    };
                    enteringTrader.Until();
                    context.OverrideNext(args.TaskName, ["进入前瞻性投资系统"]);
                    return true;
            }
            return false;
        };
        level.Until();
        return true;
    }

    public void MeetByChance(IMaaContext context, string taskName)
    {
        Thread.Sleep(1000);
        for (int i = 0; i < 3; i++)
        {
            context.Click(624, 355);
            Thread.Sleep(150);
        }
        var image = context.GetImage();
        RecognitionDetail? detail;
        int ix = 0;
        var entering = () =>
        {
            context.GetImage(ref image);
            if (context.TemplateMatch("encounter.png", image, out detail, 0.8, 545, 330, 260, 200))
            {
                ix = 0;
                MainWindow.AddLogByColor("事件: 相遇");
                return true;
            }
            if (context.TemplateMatch("disabuse.png", image, out detail, 0.8, 545, 330, 260, 200))
            {
                ix = 1;
                MainWindow.AddLogByColor("事件: 解惑");
                return true;
            }
            if (context.TemplateMatch("haunting.png", image, out detail, 0.8, 545, 330, 260, 200))
            {
                ix = 2;
                MainWindow.AddLogByColor("事件: 阴魂不散");
                return true;
            }
            if (context.TemplateMatch("fallingObject.png", image, out detail, 0.8, 545, 330, 260, 200))
            {
                ix = 3;
                MainWindow.AddLogByColor("事件: 高空坠物");
                return true;
            }
            if (context.TemplateMatch("afterStory.png", image, out detail, 0.8, 545, 330, 260, 200))
            {
                ix = 4;
                MainWindow.AddLogByColor("事件: 在故事结束之后");
                return true;
            }
            if (context.TemplateMatch("entomkaz.png", image, out detail, 0.8, 545, 330, 260, 200))
            {
                ix = 5;
                MainWindow.AddLogByColor("事件: 虫卡兹");
                return true;
            }
            return false;
        };
        entering.Until();
        HandleAllMeeting(context, taskName, ix);
        var leaving = () =>
        {
            context.GetImage(ref image);
            if (context.TemplateMatch("rougelikeInfo.png", image, out detail, 0.8, 53, 1, 178, 64))
            {
                return true;
            }
            context.Click(628, 621);
            return false;
        };
        leaving.Until();
    }
    public void HandleAllMeeting(IMaaContext context, string taskName, int i)
    {
        switch (i)
        {
            case 0:
                SelectOne(context, 3, 4);
                break;
            case 1:
                SelectOne(context, 2, 2);
                break;
            case 2:
                SelectOne(context, 2, 2);
                break;
            case 3:
                SelectOne(context, 1, 1);
                break;
            case 4:
                SelectOne(context, 1, 1);
                break;
            case 5:
                SelectOne(context, 3, 3);
                break;
        }
    }
    public bool Combat(IMaaContext context, string taskName)
    {
        IMaaImageBuffer image = new MaaImageBuffer();
        RecognitionDetail? detail;
        var shouldClose = false;
        var refresh = () =>
        {
            context.GetImage(ref image);
            LoggerService.LogInfo("刷新");
            if (context.OCR("刷新", image, out detail, 780, 90, 80, 220))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }
            return false;
        };
        refresh.Until();
        var confirmRefresh = () =>
        {
            context.GetImage(ref image);
            if (context.TemplateMatch("unableRefresh.png", image, out detail, 0.8, 945, 70, 90, 90))
            {
                shouldClose = true;
                context.OverrideNext(taskName, ["启动检测"]);
                return true;
            }
            if (context.TemplateMatch("refreshConfirm.png", image, out detail, 0.8, 690, 456, 580, 64))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }
            return false;
        };
        confirmRefresh.Until();

        return !shouldClose;
    }

    public void SelectOne(IMaaContext context, int targetOptionIndex, int totalOptions)
    {
        Thread.Sleep(1000);
        var screenHeight = 720;

        var optionHeight = 140;

        var verticalSpacing = 12.5;

        var totalOptionsHeight = totalOptions * optionHeight + (totalOptions - 1) * verticalSpacing;

        var remainingSpace = screenHeight - totalOptionsHeight;

        var topMargin = remainingSpace / 2;

        var clickY = topMargin + (targetOptionIndex - 1) * (optionHeight + verticalSpacing) + optionHeight / 2;

        var clickX = 1100;
        IMaaImageBuffer image = new MaaImageBuffer();
        Thread.Sleep(500);
        var select = () =>
        {
            RecognitionDetail? detail;
            context.GetImage(ref image);
            if (context.TemplateMatch("Sarkaz@Roguelike@StageEncounterLeaveConfirm.png", image, out detail, 0.8, 1032, 0, 247, 720))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }
            LoggerService.LogInfo($"{clickX}, {(int)clickY}");
            context.Click(clickX, (int)clickY);
            return false;
        };
        select.Until();
    }

    public int Find(IMaaContext context, int x, int y, int width, int height)
    {
        int i = 0;
        var find = () =>
        {
            RecognitionDetail? detail;
            var image = context.GetImage();
            if (context.TemplateMatch("Sarkaz@Roguelike@StageTraderCR.png", image, out detail, 0.8, x, y, width, height))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                MainWindow.AddLogByColor("关卡: 诡意行商", "ForestGreen");
                i = 2;
                return true;
            }
            if (context.TemplateMatch("Sarkaz@Roguelike@StageEncounter.png", image, out detail, 0.87, x, y, width, height))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                i = 0;
                return true;
            }
            if (context.TemplateMatch("Sarkaz@Roguelike@StageCombatDps.png", image, out detail, 0.8, x, y, width, height))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                MainWindow.AddLogByColor("关卡: 作战", "MediumPurple");
                i = 1;
                return true;
            }
            if (context.TemplateMatch("Sarkaz@Roguelike@StageEmergencyDps.png", image, out detail, 0.85, x, y, width, height))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                MainWindow.AddLogByColor("关卡: 紧急作战", "MediumPurple");
                i = 1;
                return true;
            }

            return false;
        };
        find.Until(700);
        return i;
    }
}
