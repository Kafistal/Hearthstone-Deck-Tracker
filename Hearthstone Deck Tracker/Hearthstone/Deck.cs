﻿#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Stats;

#endregion

namespace Hearthstone_Deck_Tracker.Hearthstone
{
	public class Deck : ICloneable
	{
		[XmlArray(ElementName = "Cards")]
		[XmlArrayItem(ElementName = "Card")]
		public ObservableCollection<Card> Cards;

		public string Class;
		public string HearthStatsDeckVersionId;
		public string HearthStatsId;

		[XmlIgnore]
		public bool IsSelectedInGui;

		public DateTime LastEdited;

		[XmlArray(ElementName = "MissingCards")]
		[XmlArrayItem(ElementName = "Card")]
		public List<Card> MissingCards;

		public string Note;
		public SerializableVersion SelectedVersion = new SerializableVersion(1, 0);

		[XmlArray(ElementName = "Tags")]
		[XmlArrayItem(ElementName = "Tag")]
		public List<string> Tags;

		public string Url;

		public SerializableVersion Version = new SerializableVersion(1, 0);

		[XmlArray(ElementName = "DeckHistory")]
		[XmlArrayItem(ElementName = "Deck")]
		public List<Deck> Versions;

		private Guid _deckId;


		public Deck()
		{
			Cards = new ObservableCollection<Card>();
			MissingCards = new List<Card>();
			Tags = new List<string>();
			Note = string.Empty;
			Url = string.Empty;
			Name = string.Empty;
			SyncWithHearthStats = null;
			HearthStatsId = string.Empty;
			Version = SerializableVersion.Default;
			Versions = new List<Deck>();
			DeckId = Guid.NewGuid();
		}


		public Deck(string name, string className, IEnumerable<Card> cards, IEnumerable<string> tags, string note, string url,
		            DateTime lastEdited, List<Card> missingCards, SerializableVersion version, IEnumerable<Deck> versions,
		            bool? syncWithHearthStats, string hearthStatsId, Guid deckId, bool versionOnHearthStats,
		            SerializableVersion selectedVersion = null)

		{
			Name = name;
			Class = className;
			Cards = new ObservableCollection<Card>();
			MissingCards = missingCards;
			foreach(var card in cards)
				Cards.Add((Card)card.Clone());
			Tags = new List<string>(tags);
			Note = note;
			Url = url;
			LastEdited = lastEdited;
			Version = version;
			SyncWithHearthStats = syncWithHearthStats;
			HearthStatsId = hearthStatsId;
			SelectedVersion = selectedVersion ?? version;
			Versions = new List<Deck>();
			DeckId = deckId;
			VersionOnHearthStats = versionOnHearthStats;
			if(versions != null)
			{
				foreach(var d in versions)
					Versions.Add(d.CloneWithNewId() as Deck);
			}
		}


		public Guid DeckId
		{
			get { return _deckId == Guid.Empty ? (_deckId = Guid.NewGuid()) : _deckId; }
			set { _deckId = value; }
		}

		public string Name { get; set; }
		public bool? SyncWithHearthStats { get; set; }

		[XmlIgnore]
		public bool HasHearthStatsId
		{
			get { return !string.IsNullOrEmpty(HearthStatsId); }
		}

		[XmlIgnore]
		public List<SerializableVersion> VersionsIncludingSelf
		{
			get { return Versions.Select(x => x.Version).Concat(new[] {Version}).ToList(); }
		}

		[XmlIgnore]
		public string NameAndVersion
		{
			get { return Versions.Count == 0 ? Name : string.Format("{0} (v{1}.{2})", Name, SelectedVersion.Major, SelectedVersion.Minor); }
		}

		[XmlIgnore]
		public string WinPercentString
		{
			get
			{
				if(DeckStats.Games.Count == 0)
					return "-%";
				return Math.Round(WinPercent, 0) + "%";
			}
		}

		[XmlIgnore]
		public double WinPercent
		{
			get
			{
				if(DeckStats.Games.Count == 0)
					return 0.0;
				return 100.0 * DeckStats.Games.Count(g => g.Result == GameResult.Win) / DeckStats.Games.Count;
			}
		}

		[XmlIgnore]
		public string GetClass
		{
			get { return string.IsNullOrEmpty(Class) ? "(No Class Selected)" : "(" + Class + ")"; }
		}

		[XmlIgnore]
		public string GetName
		{
			get { return Name; }
		}

		[XmlIgnore]
		public FontWeight GetFontWeight
		{
			get { return IsSelectedInGui ? FontWeights.Black : FontWeights.Bold; }
		}

		[XmlIgnore]
		public string TagList
		{
			get { return Tags.Count > 0 ? "[" + Tags.Aggregate((t, n) => t + ", " + n) + "]" : ""; }
		}

		[XmlIgnore]
		public SolidColorBrush ClassColorBrush
		{
			get { return new SolidColorBrush(ClassColor); }
		}

		[XmlIgnore]
		public Color ClassColor
		{
			get
			{
				switch(Class)
				{
					case "Druid":
						return (Color)ColorConverter.ConvertFromString("#FF7D0A");
					case "Death Knight":
						return (Color)ColorConverter.ConvertFromString("#C41F3B");
					case "Hunter":
						return (Color)ColorConverter.ConvertFromString("#ABD473");
					case "Mage":
						return (Color)ColorConverter.ConvertFromString("#69CCF0");
					case "Monk":
						return (Color)ColorConverter.ConvertFromString("#00FF96");
					case "Paladin":
						return (Color)ColorConverter.ConvertFromString("#F58CBA");
					case "Priest":
						return (Color)ColorConverter.ConvertFromString("#FFFFFF");
					case "Rogue":
						return (Color)ColorConverter.ConvertFromString("#FFF569");
					case "Shaman":
						return (Color)ColorConverter.ConvertFromString("#0070DE");
					case "Warlock":
						return (Color)ColorConverter.ConvertFromString("#9482C9");
					case "Warrior":
						return (Color)ColorConverter.ConvertFromString("#C79C6E");
					default:
						return Colors.Gray;
				}
			}
		}
		[XmlIgnore]
		public BitmapImage HeroImage
		{
			get
			{
				if(!Enum.GetNames(typeof(HeroClass)).Contains(Class))
					return new BitmapImage();
				var uri = new Uri(string.Format("../../Resources/{0}_small.png", Class.ToLower()), UriKind.Relative);
				return new BitmapImage(uri);
			}
		}
		public DeckStats DeckStats
		{
			get
			{
				var deckStats = DeckStatsList.Instance.DeckStats.FirstOrDefault(ds => ds.BelongsToDeck(this));
				if(deckStats == null)
				{
					deckStats = new DeckStats(this);
					DeckStatsList.Instance.DeckStats.Add(deckStats);
				}
				return deckStats;
			}
		}

		[XmlIgnore]
		public bool HasVersions
		{
			get { return Versions != null && Versions.Count > 0; }
		}

		public bool VersionOnHearthStats { get; set; }


		public object Clone()
		{
			return new Deck(Name, Class, Cards, Tags, Note, Url, LastEdited, MissingCards, Version, Versions, SyncWithHearthStats, HearthStatsId,
			                DeckId, VersionOnHearthStats, SelectedVersion);
		}

		public Deck GetVersion(int major, int minor)
		{
			var target = new SerializableVersion(major, minor);
			if(Version == target)
				return this;
			return Versions.FirstOrDefault(x => x.Version == target);
		}

		public bool HasVersion(SerializableVersion version)
		{
			return Version == version || Versions.Any(v => v.Version == version);
		}

		public object CloneWithNewId()
		{
			return new Deck(Name, Class, Cards, Tags, Note, Url, LastEdited, MissingCards, Version, Versions, SyncWithHearthStats, "",
			                Guid.NewGuid(), VersionOnHearthStats, SelectedVersion);
		}

		public void ResetVersions()
		{
			Versions = new List<Deck>();
			Version = SerializableVersion.Default;
			SelectedVersion = Version;
		}

		public Deck GetSelectedDeckVersion()
		{
			return Versions == null ? this : Versions.FirstOrDefault(d => d.Version == SelectedVersion) ?? this;
		}

		public void SelectVersion(SerializableVersion version)
		{
			SelectedVersion = version;
		}

		public void SelectVersion(Deck deck)
		{
			SelectVersion(deck.Version);
		}

		public string GetDeckInfo()
		{
			return string.Format("deckname:{0}, class:{1}, cards:{2}", Name, Class, Cards.Sum(x => x.Count));
		}

		/// returns the number of cards in the deck with mechanics matching the newmechanic.
		/// The mechanic attribute, such as windfury or taunt, comes from the cardDB json file
		public int getMechanicCount(String newmechanic)
		{
			int count;

			count = 0;
			foreach(var card in Cards)
			{
				if(card.Mechanics != null)
				{
					foreach(var mechanic in card.Mechanics)
					{
						if(mechanic.Equals(newmechanic))
							count++;
					}
				}
			}

			Console.WriteLine(newmechanic + count + "\n");
			return count;
		}

		public int getNumTaunt()
		{
			return getMechanicCount("Taunt");
		}

		public int getNumBattlecry()
		{
			return getMechanicCount("Battlecry");
		}

		public int getNumImmuneToSpellpower()
		{
			return getMechanicCount("ImmuneToSpellpower");
		}

		public int getNumSpellpower()
		{
			return getMechanicCount("Spellpower");
		}

		public int getNumOneTurnEffect()
		{
			return getMechanicCount("OneTurnEffect");
		}

		public int getNumCharge()
		{
			return getMechanicCount("Charge") + getMechanicCount("GrantCharge");
		}

		public int getNumFreeze()
		{
			return getMechanicCount("Freeze");
		}

		public int getNumAdjacentBuff()
		{
			return getMechanicCount("AdjacentBuff");
		}

		public int getNumSecret()
		{
			return getMechanicCount("Secret");
		}

		public int getNumDeathrattle()
		{
			return getMechanicCount("Deathrattle");
		}

		public int getNumWindfury()
		{
			return getMechanicCount("Windfury");
		}

		public int getNumDivineShield()
		{
			return getMechanicCount("Divine Shield");
		}

		public int getNumCombo()
		{
			return getMechanicCount("Combo");
		}

		public override string ToString()
		{
			return string.Format("{0} ({1})", Name, Class);
		}

		public override bool Equals(object obj)
		{
			var deck = obj as Deck;
			if(deck == null)
				return false;
			if(deck.HasHearthStatsId && HasHearthStatsId)
				return HearthStatsId.Equals(deck.HearthStatsId);
			return DeckId.Equals(deck.DeckId);
		}

		public override int GetHashCode()
		{
			if(HasHearthStatsId)
				return HasHearthStatsId.GetHashCode();
			return DeckId.GetHashCode();
		}

		public static List<Card> operator -(Deck first, Deck second)
		{
			var result = new Deck();

			var diff = new List<Card>();
			//removed
			//diff.AddRange(prevVersion.Cards.Where(c => !selected.Cards.Contains(c)));
			foreach(var c in second.Cards.Where(c => !first.Cards.Contains(c)))
			{
				var cd = c.Clone() as Card;
				cd.Count = -cd.Count; //merk as negative for visual
				diff.Add(cd);
			}
			//added
			diff.AddRange(first.Cards.Where(c => !second.Cards.Contains(c)));

			//diff count
			var diffCount =
				first.Cards.Where(c => second.Cards.Any(c2 => c2.Id == c.Id) && second.Cards.First(c2 => c2.Id == c.Id).Count != c.Count);
			foreach(var card in diffCount)
			{
				var cardclone = card.Clone() as Card;
				cardclone.Count = cardclone.Count - second.Cards.Where(c => c.Id == cardclone.Id).First().Count;
				diff.Add(cardclone);
			}

			return diff;
		}
	}
}