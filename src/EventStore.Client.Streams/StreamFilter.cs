using System;
using System.Linq;
using System.Text.RegularExpressions;

#nullable enable
namespace EventStore.Client {
	public readonly struct StreamFilter : IEquatable<StreamFilter>, IEventFilter {
		public static readonly StreamFilter None = default;

		private readonly PrefixFilterExpression[] _prefixes;

		public PrefixFilterExpression[] Prefixes => _prefixes ?? Array.Empty<PrefixFilterExpression>();
		public RegularFilterExpression Regex { get; }
		public uint? MaxSearchWindow { get; }

		public static IEventFilter Prefix(string prefix)
			=> new StreamFilter(new PrefixFilterExpression(prefix));

		public static IEventFilter Prefix(params string[] prefixes)
			=> new StreamFilter(Array.ConvertAll(prefixes, prefix => new PrefixFilterExpression(prefix)));

		public static IEventFilter Prefix(uint maxSearchWindow, params string[] prefixes)
			=> new StreamFilter(maxSearchWindow,
				Array.ConvertAll(prefixes, prefix => new PrefixFilterExpression(prefix)));

		public static IEventFilter RegularExpression(string regex, uint maxSearchWindow = 32)
			=> new StreamFilter(maxSearchWindow, new RegularFilterExpression(regex));

		public static IEventFilter RegularExpression(Regex regex, uint maxSearchWindow = 32)
			=> new StreamFilter(maxSearchWindow, new RegularFilterExpression(regex));


		private StreamFilter(RegularFilterExpression regex) : this(default, regex) { }

		private StreamFilter(uint maxSearchWindow, RegularFilterExpression regex) {
			if (maxSearchWindow == 0) {
				throw new ArgumentOutOfRangeException(nameof(maxSearchWindow),
					maxSearchWindow, $"{nameof(maxSearchWindow)} must be greater than 0.");
			}

			Regex = regex;
			_prefixes = Array.Empty<PrefixFilterExpression>();
			MaxSearchWindow = maxSearchWindow;
		}

		private StreamFilter(params PrefixFilterExpression[] prefixes) : this(32, prefixes) { }

		private StreamFilter(uint maxSearchWindow, params PrefixFilterExpression[] prefixes) {
			if (prefixes.Length == 0) {
				throw new ArgumentException();
			}

			if (maxSearchWindow == 0) {
				throw new ArgumentOutOfRangeException(nameof(maxSearchWindow),
					maxSearchWindow, $"{nameof(maxSearchWindow)} must be greater than 0.");
			}

			_prefixes = prefixes;
			Regex = RegularFilterExpression.None;
			MaxSearchWindow = maxSearchWindow;
		}

		public bool Equals(StreamFilter other) =>
			Prefixes.SequenceEqual(other.Prefixes) &&
			Regex.Equals(other.Regex) &&
			MaxSearchWindow.Equals(other.MaxSearchWindow);

		public override bool Equals(object? obj) => obj is StreamFilter other && Equals(other);
		public override int GetHashCode() => HashCode.Hash.Combine(Prefixes).Combine(Regex).Combine(MaxSearchWindow);
		public static bool operator ==(StreamFilter left, StreamFilter right) => left.Equals(right);
		public static bool operator !=(StreamFilter left, StreamFilter right) => !left.Equals(right);

		public override string ToString() =>
			this == None
				? "(none)"
				: $"{nameof(StreamFilter)} {(Prefixes.Length == 0 ? Regex.ToString() : $"[{string.Join(", ", Prefixes)}]")}";
	}
}
