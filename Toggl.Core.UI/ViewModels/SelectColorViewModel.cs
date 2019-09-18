using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Services;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Parameters;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Colors = Toggl.Core.UI.Helper.Colors;

namespace Toggl.Core.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public class SelectColorViewModel : ViewModel<ColorParameters, Color>
    {
        private Color defaultColor;
        private readonly ISchedulerProvider schedulerProvider;

        private BehaviorSubject<float> hue { get; } = new BehaviorSubject<float>(0.0f);
        private BehaviorSubject<float> value { get; } = new BehaviorSubject<float>(0.375f);
        private BehaviorSubject<float> saturation { get; } = new BehaviorSubject<float>(0.0f);

        public bool AllowCustomColors { get; private set; }

        public IObservable<float> Hue { get; }
        public IObservable<float> Value { get; }
        public IObservable<float> Saturation { get; }
        public IObservable<IImmutableList<SelectableColorViewModel>> SelectableColors { get; private set; }

        public UIAction Save { get; }
        public InputAction<float> SetHue { get; }
        public InputAction<float> SetSaturation { get; }
        public InputAction<float> SetValue { get; }
        public RxAction<Color, Color> SelectColor { get; }

        public SelectColorViewModel(
            INavigationService navigationService,
            IRxActionFactory rxActionFactory,
            ISchedulerProvider schedulerProvider)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.schedulerProvider = schedulerProvider;

            Hue = hue.AsDriver(schedulerProvider);
            Saturation = saturation.AsDriver(schedulerProvider);
            Value = value.AsDriver(schedulerProvider);

            Save = rxActionFactory.FromAsync(save);
            SetHue = rxActionFactory.FromAction<float>(hue.OnNext);
            SetSaturation = rxActionFactory.FromAction<float>(saturation.OnNext);
            SetValue = rxActionFactory.FromAction<float>(value.OnNext);
            SelectColor = rxActionFactory.FromFunction<Color, Color>(selectColor);
        }

        public override Task Initialize(ColorParameters parameter)
        {
            defaultColor = parameter.Color;
            AllowCustomColors = parameter.AllowCustomColors;

            var selectableColorObservable = Observable.Defer(() =>
            {
                if (!AllowCustomColors)
                    return Observable.Return(Colors.DefaultProjectColors);

                return Observable
                    .CombineLatest(hue, saturation, value, Colors.FromHSV)
                    .Throttle(TimeSpan.FromMilliseconds(100), schedulerProvider.DefaultScheduler)
                    .Do(SelectColor.Inputs)
                    .Select(availableColors);
            });

            var selectedColorObservable = Observable.Defer(() =>
            {
                var defaultColorSelected = Colors.DefaultProjectColors
                    .Any(color => color == defaultColor);
                if (defaultColorSelected)
                {
                    if (!AllowCustomColors)
                        return Observable.Return(defaultColor);

                    SelectColor.Execute(defaultColor);
                    return SelectColor.Elements;
                }

                if (!AllowCustomColors)
                {
                    var defaultSelectedColor = Colors.DefaultProjectColors.First();
                    return Observable.Return(defaultSelectedColor);
                }

                var colorComponents = defaultColor.GetHSV();
                hue.OnNext(colorComponents.hue);
                value.OnNext(colorComponents.value);
                saturation.OnNext(colorComponents.saturation);
                return SelectColor.Elements;
            });

            SelectableColors = selectableColorObservable
                .CombineLatest(selectedColorObservable, updateSelectableColors)
                .AsDriver(schedulerProvider);

            return base.Initialize(parameter);

            IEnumerable<Color> availableColors(Color customColor)
            {
                foreach (var color in Colors.DefaultProjectColors)
                {
                    yield return color;
                }

                yield return customColor;
            }

            IImmutableList<SelectableColorViewModel> updateSelectableColors(IEnumerable<Color> colors, Color selectedColor)
                => colors
                    .Select(color => new SelectableColorViewModel(color, color == selectedColor))
                    .ToImmutableList();
        }

        public override void CloseWithDefaultResult()
        {
            Close(defaultColor);
        }

        private Color selectColor(Color color)
        {
            if (!AllowCustomColors)
                Close(color);

            return color;
        }

        private async Task save()
        {
            var colors = await SelectableColors.FirstAsync();
            var selectedColor = colors.First(c => c.Selected).Color;
            Close(selectedColor);
        }
    }
}
