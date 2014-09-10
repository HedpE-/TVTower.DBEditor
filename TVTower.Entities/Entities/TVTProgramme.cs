﻿using System.Collections.Generic;
using System.Linq;

namespace TVTower.Entities
{
	public class TVTProgramme : TVTEntity, ITVTProgrammeCore
	{
		public TVTProgrammeType ProgrammeType { get; set; }

		public string TitleDE { get; set; }
		public string TitleEN { get; set; }
		public string DescriptionDE { get; set; }
		public string DescriptionEN { get; set; }

		public string FakeTitleDE { get; set; }
		public string FakeTitleEN { get; set; }

		public string FakeDescriptionDE { get; set; } //Optional
		public string FakeDescriptionEN { get; set; } //Optional

		public string DescriptionMovieDB { get; set; }

		public IndexingList<TVTStaff> Staff { get; set; }

		public int BettyBonus { get; set; }		//0 - 10
		public int PriceMod { get; set; }		//0 - 255
		public int CriticsRate { get; set; }	//0 - 255
		public int ViewersRate { get; set; }	//0 - 255	-	auch als Speed bekannt.
		public int BoxOfficeRate { get; set; }	//0 - 255	-	auch als Outcome bekannt.     

		//Fields not for episodes
		public string Country { get; set; }				//ISO-3166-1 ALPHA-2    (http://de.wikipedia.org/wiki/ISO-3166-1-Kodierliste)
		public int Year { get; set; }					//YYYY   = 1900+
		public TVTDistributionChannel DistributionChannel { get; set; }

		public TVTProgrammeGenre MainGenre { get; set; }
		public TVTProgrammeGenre SubGenre { get; set; }

		public int Blocks { get; set; }					//1 - 5
		public int? LiveHour { get; set; }				//0 - 23

		public List<TVTMovieFlag> Flags { get; set; }
		public List<TVTTargetGroup> TargetGroups { get; set; }
		public List<TVTPressureGroup> ProPressureGroups { get; set; }
		public List<TVTPressureGroup> ContraPressureGroups { get; set; }

		public string ImdbId { get; set; }				//IMDb = Internet Movie Database
		public int? TmdbId { get; set; }					//The Movie DB		
		public int? RottenTomatoesId { get; set; }		//rottentomatoes.com
		public string ImageUrl { get; set; }			//Von hier kann die Bildquelle geladen werden

		//Für Serien
		public List<TVTEpisode> Episodes { get; set; }
		public TVTMovieAdditional MovieAdditional { get; set; }


		public TVTProgramme()
		{
			Staff = new IndexingList<TVTStaff>();
			Flags = new List<TVTMovieFlag>();
			TargetGroups = new List<TVTTargetGroup>();
		}

		public override TVTDataStatus RefreshStatus()
		{
			var baseStatus = base.RefreshStatus();
			if ( baseStatus == TVTDataStatus.Incorrect )
				return baseStatus;

			if ( ProgrammeType == TVTProgrammeType.Series )
			{
				var EpisodesIndexes = new List<int>();

				if ( Episodes.Count == 0 )
					return TVTDataStatus.Incomplete;

				foreach ( var episode in Episodes )
				{
					var episodeStatus = episode.RefreshStatus();
					if ( episodeStatus == TVTDataStatus.Incorrect || episodeStatus == TVTDataStatus.Incomplete )
					{
						DataStatus = episodeStatus;
						return DataStatus;
					}

					if ( !episode.EpisodeIndex.HasValue || EpisodesIndexes.Contains( episode.EpisodeIndex.Value ) )
					{
						DataStatus = TVTDataStatus.Incorrect;
						return DataStatus;
					}
					else
						EpisodesIndexes.Add( episode.EpisodeIndex.Value );
				}
			}

			if ( string.IsNullOrEmpty( Country ) ||
				MainGenre == TVTProgrammeGenre.Undefined ||
				MainGenre == TVTProgrammeGenre.Undefined_Show ||
				MainGenre == TVTProgrammeGenre.Undefined_Reportage ||
				Blocks == 0 ||
				Year == 0 )
			{
				DataStatus = TVTDataStatus.Incomplete;
				return DataStatus;
			}
			else
			{
				if ( !string.IsNullOrEmpty( TitleDE ) &&
					!string.IsNullOrEmpty( DescriptionDE ) &&
					!string.IsNullOrEmpty( FakeTitleDE ) )
				{
					DataStatus = TVTDataStatus.OnlyDE;
					return DataStatus;
				}

				if ( !string.IsNullOrEmpty( TitleEN ) &&
					!string.IsNullOrEmpty( DescriptionEN ) &&
					!string.IsNullOrEmpty( FakeTitleEN ) )
				{
					DataStatus = TVTDataStatus.OnlyEN;
					return DataStatus;
				}

				//if ( string.IsNullOrEmpty( TitleDE ) ||
				//    string.IsNullOrEmpty( DescriptionDE ) ||
				//    string.IsNullOrEmpty( TitleEN ) ||
				//    string.IsNullOrEmpty( DescriptionEN ) )
				//{
				//    DataStatus = TVTDataStatus.Incomplete;

				if ( DataType != TVTDataType.Fictitious )
				{
					if ( string.IsNullOrEmpty( FakeTitleDE ) ||
						string.IsNullOrEmpty( FakeTitleEN ) )
					{
						DataStatus = TVTDataStatus.NoFakes;
					}
				}
				//}
			}



			return DataStatus;
		}

		public override void RefreshReferences( ITVTDatabase database )
		{
			if ( this.ProgrammeType == TVTProgrammeType.Series )
			{
				Episodes = database.GetEpisodesOfSeries( this.Id ).OrderBy( x => x.EpisodeIndex ).ToList<TVTEpisode>();
			}
		}
	}
}
