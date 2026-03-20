using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class PopupPanel
    {
        public VisualElement Overlay { get; }

        private readonly VisualElement _pagesContainer;
        private readonly VisualElement _dotContainer;
        private readonly Button _prevBtn;
        private readonly Button _nextBtn;
        private readonly List<VisualElement> _pages = new();
        private int _currentPage;

        public PopupPanel(string title, string footerText = null)
        {
            Overlay = new VisualElement { name = "doc-overlay" };
            Overlay.AddToClassList("doc-overlay");
            Overlay.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == Overlay) Close();
            });

            var panel = new VisualElement();
            panel.AddToClassList("doc-panel");

            // Header
            var header = new VisualElement();
            header.AddToClassList("doc-panel__header");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("doc-panel__title");
            header.Add(titleLabel);

            var closeBtn = new Button(Close);
            closeBtn.text = "\u00d7";
            closeBtn.AddToClassList("doc-panel__close-btn");
            header.Add(closeBtn);
            panel.Add(header);

            // Pages
            _pagesContainer = new VisualElement();
            _pagesContainer.AddToClassList("doc-panel__pages");
            panel.Add(_pagesContainer);

            // Footer
            var footer = new VisualElement();
            footer.AddToClassList("doc-panel__footer");

            _prevBtn = new Button(() => Navigate(-1));
            _prevBtn.text = "\u25c0  PREV";
            _prevBtn.AddToClassList("doc-panel__nav-btn");
            footer.Add(_prevBtn);

            _dotContainer = new VisualElement();
            _dotContainer.AddToClassList("doc-panel__page-indicator");
            footer.Add(_dotContainer);

            _nextBtn = new Button(() => Navigate(1));
            _nextBtn.text = "NEXT  \u25b6";
            _nextBtn.AddToClassList("doc-panel__nav-btn");
            footer.Add(_nextBtn);

            panel.Add(footer);
            Overlay.Add(panel);
        }

        public void AddPage(VisualElement page)
        {
            page.AddToClassList("doc-page");
            _pages.Add(page);
            _pagesContainer.Add(page);

            var dot = new VisualElement();
            dot.AddToClassList("doc-panel__page-dot");
            _dotContainer.Add(dot);
        }

        public void Show(VisualElement parent)
        {
            _currentPage = 0;
            parent.Add(Overlay);
            UpdatePage();
        }

        public void Close()
        {
            Overlay.RemoveFromHierarchy();
        }

        private void Navigate(int delta)
        {
            _currentPage = Mathf.Clamp(_currentPage + delta, 0, _pages.Count - 1);
            UpdatePage();
        }

        private void UpdatePage()
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (i == _currentPage)
                    _pages[i].AddToClassList("doc-page--active");
                else
                    _pages[i].RemoveFromClassList("doc-page--active");
            }

            for (int i = 0; i < _dotContainer.childCount; i++)
            {
                if (i == _currentPage)
                    _dotContainer[i].AddToClassList("doc-panel__page-dot--active");
                else
                    _dotContainer[i].RemoveFromClassList("doc-panel__page-dot--active");
            }

            if (_currentPage == 0)
                _prevBtn.AddToClassList("doc-panel__nav-btn--hidden");
            else
                _prevBtn.RemoveFromClassList("doc-panel__nav-btn--hidden");

            if (_currentPage == _pages.Count - 1)
                _nextBtn.AddToClassList("doc-panel__nav-btn--hidden");
            else
                _nextBtn.RemoveFromClassList("doc-panel__nav-btn--hidden");
        }
    }
}
