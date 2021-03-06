﻿using MoneyFox.Business.ViewModels.Interfaces;
using MoneyFox.Foundation.Models;
using MvvmCross.Core.ViewModels;

namespace MoneyFox.Business.ViewModels.DesignTime
{
    public class DesignTimeStatisticCashFlowViewModel : IStatisticCashFlowViewModel
    {
        public string Title => "I AM A MIGHTY TITLE";

        public MvxObservableCollection<StatisticItem> StatisticItems => new MvxObservableCollection<StatisticItem>
        {
            new StatisticItem {Label = "Expense", Value = 1234},
            new StatisticItem {Label = "Income", Value = 1465},
            new StatisticItem {Label = "Revenue", Value = 543},
        };

    }
}
