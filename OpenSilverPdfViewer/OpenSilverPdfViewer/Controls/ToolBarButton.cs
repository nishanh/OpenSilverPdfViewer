using System.Windows;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace OpenSilverPdfViewer.Controls
{
    internal sealed class ToolBarButton : ButtonBase
    {
        #region Dependency Properties

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ToolBarButton),
          new PropertyMetadata(new CornerRadius()));

        public static readonly DependencyProperty DisabledRadiusProperty = DependencyProperty.Register("DisabledRadius", typeof(CornerRadius), typeof(ToolBarButton),
          new PropertyMetadata(new CornerRadius()));

        public static readonly DependencyProperty DisabledMarginProperty = DependencyProperty.Register("DisabledMargin", typeof(Thickness), typeof(ToolBarButton),
          new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty ShadowThicknessProperty = DependencyProperty.Register("ShadowThickness", typeof(Thickness), typeof(ToolBarButton),
          new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty ContentHighlightColorProperty = DependencyProperty.Register("ContentHighlightColor", typeof(Color), typeof(ToolBarButton),
          new PropertyMetadata(Colors.White));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        public CornerRadius DisabledRadius
        {
            get => (CornerRadius)GetValue(DisabledRadiusProperty);
            set => SetValue(DisabledRadiusProperty, value);
        }
        public Thickness ShadowThickness
        {
            get => (Thickness)GetValue(ShadowThicknessProperty);
            set => SetValue(ShadowThicknessProperty, value);
        }
        public Thickness DisabledMargin
        {
            get => (Thickness)GetValue(DisabledMarginProperty);
            set => SetValue(DisabledMarginProperty, value);
        }
        public Color ContentHighlightColor
        {
            get => (Color)GetValue(ContentHighlightColorProperty);
            set => SetValue(ContentHighlightColorProperty, value);
        }

        #endregion Dependency Properties
        static ToolBarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(typeof(ToolBarButton)));
        }
    }
}
