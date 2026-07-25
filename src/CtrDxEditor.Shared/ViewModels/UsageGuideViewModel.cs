using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using CtrDxEditor.UsageGuide;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Search and browser-like navigation state for the built-in Usage Guide.</summary>
    public sealed class UsageGuideViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, GuideArticle> _byId;
        private readonly string _homeArticleId;

        /// <summary>Creates guide state over an article catalog.</summary>
        /// <param name="articles">Complete ordered article catalog.</param>
        /// <param name="homeArticleId">Stable identifier selected initially and by <see cref="GoHome"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="articles"/> is empty or does not contain
        /// <paramref name="homeArticleId"/>.
        /// </exception>
        public UsageGuideViewModel(IReadOnlyList<GuideArticle> articles, string homeArticleId)
        {
            if (articles.Count == 0)
            {
                throw new ArgumentException("The Usage Guide requires at least one article.", nameof(articles));
            }

            Articles = articles;
            _byId = articles.ToDictionary(article => article.Id, StringComparer.Ordinal);
            if (!_byId.TryGetValue(homeArticleId, out GuideArticle? home))
            {
                throw new ArgumentException("The home article must exist in the catalog.", nameof(homeArticleId));
            }

            _homeArticleId = homeArticleId;
            SelectedArticle = home;
            FilteredArticles = [.. articles];
        }

        /// <summary>Raised when presentation state changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>The complete table of contents.</summary>
        public IReadOnlyList<GuideArticle> Articles { get; }

        /// <summary>Articles matching <see cref="SearchText"/>.</summary>
        public ObservableCollection<GuideArticle> FilteredArticles { get; }

        /// <summary>Previously visited articles, nearest destination on top.</summary>
        private Stack<GuideArticle> Back { get; } = [];

        /// <summary>Articles left by backward navigation, nearest destination on top.</summary>
        private Stack<GuideArticle> Forward { get; } = [];

        /// <summary>The article shown in the reading pane.</summary>
        public GuideArticle SelectedArticle
        {
            get;
            private set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Text used to filter the table of contents.</summary>
        public string SearchText
        {
            get;
            set
            {
                value ??= string.Empty;
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
                ApplySearch();
            }
        } = string.Empty;

        /// <summary>Whether a previously visited article is available.</summary>
        public bool CanGoBack => Back.Count > 0;

        /// <summary>Whether an article left by going back is available.</summary>
        public bool CanGoForward => Forward.Count > 0;

        /// <summary>Navigates to an article by stable identifier.</summary>
        /// <param name="articleId">Destination identifier; unknown or current identifiers are ignored.</param>
        public void NavigateTo(string articleId)
        {
            if (!_byId.TryGetValue(articleId, out GuideArticle? destination)
                || ReferenceEquals(destination, SelectedArticle))
            {
                return;
            }

            Back.Push(SelectedArticle);
            Forward.Clear();
            SelectedArticle = destination;
            RaiseHistoryChanged();
        }

        /// <summary>Returns to the previous article.</summary>
        public void GoBack()
        {
            if (Back.TryPop(out GuideArticle? destination))
            {
                Forward.Push(SelectedArticle);
                SelectedArticle = destination;
                RaiseHistoryChanged();
            }
        }

        /// <summary>Revisits the article most recently left by going back.</summary>
        public void GoForward()
        {
            if (Forward.TryPop(out GuideArticle? destination))
            {
                Back.Push(SelectedArticle);
                SelectedArticle = destination;
                RaiseHistoryChanged();
            }
        }

        /// <summary>Navigates to the configured welcome article.</summary>
        public void GoHome()
        {
            NavigateTo(_homeArticleId);
        }

        /// <summary>
        /// Rebuilds <see cref="FilteredArticles"/> from the trimmed query and every localized searchable
        /// article field.
        /// </summary>
        private void ApplySearch()
        {
            string query = SearchText.Trim();
            IEnumerable<GuideArticle> matches = Articles;
            if (query.Length > 0)
            {
                matches = Articles.Where(article =>
                    Contains(article.Title, query)
                    || Contains(article.Section, query)
                    || Contains(article.Summary, query)
                    || article.SearchTerms.Any(term => Contains(term, query))
                    || article.Blocks.OfType<GuideParagraph>().Any(block => Contains(block.Text, query))
                    || article.Blocks.OfType<GuideHeading>().Any(block => Contains(block.Text, query))
                    || article.Blocks.OfType<GuideShortcutTable>().Any(
                        table => table.Items.Any(
                            shortcut => Contains(shortcut.Action, query) || Contains(shortcut.Keys, query))));
            }

            FilteredArticles.Clear();
            foreach (GuideArticle article in matches)
            {
                FilteredArticles.Add(article);
            }
        }

        /// <summary>Tests a localized value for an ordinal, case-insensitive query match.</summary>
        /// <param name="value">Localized text to inspect.</param>
        /// <param name="query">Non-empty trimmed search query.</param>
        /// <returns><see langword="true"/> when <paramref name="query"/> occurs in <paramref name="value"/>.</returns>
        private static bool Contains(string value, string query)
        {
            return value.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Notifies bindings that both history-derived availability flags may have changed.</summary>
        private void RaiseHistoryChanged()
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        /// <summary>Raises <see cref="PropertyChanged"/> for a changed presentation property.</summary>
        /// <param name="propertyName">
        /// Changed property name, supplied automatically by the compiler when omitted.
        /// </param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
