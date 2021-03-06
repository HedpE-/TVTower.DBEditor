﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;
using TVTower.Entities;
using CodeKnight.Core;

namespace TVTower.Xml
{
	public class XmlPersisterV3
	{
		public const int CURRENT_VERSION = 3;

		public void SaveXML( ITVTDatabase database, string filename, DatabaseVersion dbVersion, DataStructure dataStructure, bool onlyApproved = true )
		{
			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();

			XmlDocument doc = new XmlDocument();

			var declaration = doc.CreateXmlDeclaration( "1.0", "utf-8", null );
			doc.AppendChild( declaration );

			var tvgdb = doc.CreateElement( "tvgdb" );
			doc.AppendChild( tvgdb );

			var version = doc.CreateElement( "version" );
			version.AddAttribute( "value", ((int)dbVersion).ToString() );
			version.AddAttribute( "comment", "Export from TVTowerDBEditor" );
			version.AddAttribute( "exportDate", DateTime.Now.ToString() );
			tvgdb.AppendChild( version );

			{
				var allmovies = doc.CreateElement( "allprogrammes" );
				//allmovies.AddElement( "version", CURRENT_VERSION.ToString() );
				tvgdb.AppendChild( allmovies );

				foreach ( var movie in database.GetAllProgrammes( true, false ) )
				{
					if ( movie.Approved || !onlyApproved )
						SetProgrammeDetailNode( doc, allmovies, movie, dbVersion, dataStructure );
				}
			}

			//{
			//    var allepisodes = doc.CreateElement( "allepisodes" );
			//    //allmovies.AddElement( "version", CURRENT_VERSION.ToString() );
			//    tvgdb.AppendChild( allepisodes );

			//    foreach ( var episode in database.GetAllEpisodes() )
			//    {
			//        SetEpisodeDetailNode( doc, allepisodes, episode, dbVersion, dataStructure );
			//    }
			//}

			if ( ((int)dbVersion) >= 3 )
			{
				var allpeople = doc.CreateElement( "celebritypeople" );
				//allpeople.AddElement( "version", CURRENT_VERSION.ToString() );
				tvgdb.AppendChild( allpeople );

				foreach ( var person in database.GetAllPeople() )
				{
					person.RefreshStatus();

					if ( (person.DataStatus == TVTDataStatus.Approved && person.Approved || !onlyApproved) && person.Prominence != 3 )
						SetCelebrityPersonDetailNode( doc, allpeople, person, dbVersion, dataStructure );
				}
			}

			if ( ((int)dbVersion) >= 3 )
			{
				var allpeople = doc.CreateElement( "insignificantpeople" );
				//allpeople.AddElement( "version", CURRENT_VERSION.ToString() );
				tvgdb.AppendChild( allpeople );

				foreach ( var person in database.GetAllPeople() )
				{
					if ( person.Prominence == 3 )
					{
						if ( (person.DataStatus == TVTDataStatus.Approved && person.Approved || !onlyApproved) && person.Prominence == 3 )
							SetInsignificantPersonDetailNode( doc, allpeople, person, dbVersion, dataStructure );
					}
				}
			}

			if ( ((int)dbVersion) >= 3 )
			{
				var allads = doc.CreateElement( "allads" );
				//allpeople.AddElement( "version", CURRENT_VERSION.ToString() );
				tvgdb.AppendChild( allads );

				foreach ( var ad in database.GetAllAdvertisings() )
				{
					if ( ad.Approved || !onlyApproved )
						SetAdvertisingDetailNode( doc, allads, ad, dbVersion, dataStructure );
				}
			}

			if ( ((int)dbVersion) >= 3 )
			{
				var allnews = doc.CreateElement( "allnews" );
				//allpeople.AddElement( "version", CURRENT_VERSION.ToString() );
				tvgdb.AppendChild( allnews );

				foreach ( var news in database.GetAllNews() )
				{
					if ( news.Approved || !onlyApproved )
						SetNewsDetailNode( doc, allnews, news, dbVersion, dataStructure );
				}
			}

			var exportOptions = doc.CreateElement( "exportOptions" );
			exportOptions.AddAttribute( "onlyFakes", (dataStructure == DataStructure.FakeData).ToString().ToLower() );
			exportOptions.AddAttribute( "onlyCustom", "false" );
			exportOptions.AddAttribute( "dataStructure", dataStructure.ToString() );
			tvgdb.AppendChild( exportOptions );

			stopWatch.Stop();

			var time = doc.CreateElement( "time" );
			time.AddAttribute( "value", stopWatch.ElapsedMilliseconds.ToString() + "ms" );
			tvgdb.AppendChild( time );

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "	";

			using ( XmlWriter writer = XmlWriter.Create( filename, settings ) )
			{
				doc.Save( writer );
			}
		}

		public XmlNode SetAdvertisingDetailNode( XmlDocument doc, XmlElement element, TVTAdvertising ad, DatabaseVersion dbVersion, DataStructure dataStructure )
		{
			XmlNode adNode = null;

			adNode = doc.CreateElement( "ad" );
			{
				adNode.AddAttribute( "id", ad.Id.ToString() );
				adNode.AddAttribute( "creator", ad.CreatorId );
				adNode.AddAttribute( "lastmodified", ad.LastModified != null ? ad.LastModified.ToString() : "" );
			}
			element.AppendChild( adNode );

			SetTitleAndDescriptionMinimal( doc, adNode, dataStructure, ad );

			var restrictionsNode = doc.CreateElement( "restrictions" );
			{
				restrictionsNode.AddAttribute( "valid_from", ad.ValidFrom.ToString() );
				restrictionsNode.AddAttribute( "valid_till", ad.ValidTill.ToString() );
			}
			adNode.AppendChild( restrictionsNode );

			var conditionsNode = doc.CreateElement( "conditions" );
			{
				conditionsNode.AddAttribute( "min_audience", ad.MinAudience.ToString( CultureInfo.InvariantCulture ) );
				conditionsNode.AddAttribute( "min_image", ad.MinImage.ToString() );
				conditionsNode.AddAttribute( "target_group", ((int)ad.TargetGroup).ToString() );

				if ( ad.AllowedGenres != null )
				{
					foreach ( var genre in ad.AllowedGenres )
					{
						conditionsNode.AddElement( "allowed_genre", genre.ToString() );
					}
				}

				if ( ad.ProhibitedGenres != null )
				{
					foreach ( var genre in ad.ProhibitedGenres )
					{
						conditionsNode.AddElement( "prohibited_genre", genre.ToString() );
					}
				}

				if ( ad.AllowedProgrammeTypes != null )
				{
					foreach ( var type in ad.AllowedProgrammeTypes )
					{
						conditionsNode.AddElement( "allowed_programme_type", type.ToString() );
					}
				}

				if ( ad.ProhibitedProgrammeTypes != null )
				{
					foreach ( var type in ad.ProhibitedProgrammeTypes )
					{
						conditionsNode.AddElement( "prohibited_programme_type", type.ToString() );
					}
				}
			}
			adNode.AppendChild( conditionsNode );


			{
				XmlNode dataNode = doc.CreateElement( "data" );
				//dataNode.AddAttribute( "infomercial", ad.Infomercial ? "1" : "0" );
				dataNode.AddAttribute( "quality", ad.Quality.ToString() );
				dataNode.AddAttribute( "repetitions", ad.Repetitions.ToString() );
				dataNode.AddAttribute( "duration", ad.Duration.ToString() );
				dataNode.AddAttribute( "fix_price", ad.FixPrice ? "1" : "0" );
				dataNode.AddAttribute( "profit", ad.Profit.ToString() );
				dataNode.AddAttribute( "penalty", ad.Penalty.ToString() );

				dataNode.AddAttribute( "pro_pressure_groups", ad.ProPressureGroups.Sum( x => (int)x ).ToString() );
				dataNode.AddAttribute( "contra_pressure_groups", ad.ContraPressureGroups.Sum( x => (int)x ).ToString() );

				adNode.AppendChild( dataNode );
			}

			return adNode;
		}

		public XmlNode SetNewsDetailNode( XmlDocument doc, XmlElement element, TVTNews news, DatabaseVersion dbVersion, DataStructure dataStructure )
		{
			XmlNode newsNode = null;

			newsNode = doc.CreateElement( "news" );
			{
				newsNode.AddAttribute( "id", news.Id.ToString() );
				newsNode.AddAttribute( "thread_id", news.NewsThreadId );
				newsNode.AddAttribute( "type", ((int)news.NewsType).ToString() );
				newsNode.AddAttribute( "creator", news.CreatorId );
				//adNode.AddAttribute("predecessor_id", news.Predecessor != null ? news.Predecessor.Id.ToString() : null);
			}
			element.AppendChild( newsNode );

			SetTitleAndDescriptionBasic( doc, newsNode, news );

			var effectsNode = doc.CreateElement( "effects" );
			{
				if ( news.Effects != null )
				{
					foreach ( var effect in news.Effects )
					{
						var effectNode = doc.CreateElement( "effect" );
						effectNode.AddAttribute( "type", effect.Type.ToString().ToLower() );
						if ( effect.Chance != 100 )
							effectNode.AddAttribute( "chance", effect.Chance.ToString() );
						if ( effect.EffectParameters.Count == 1 )
							effectNode.AddAttribute( "parameter1", effect.EffectParameters[0] );
						if ( effect.EffectParameters.Count == 2 )
							effectNode.AddAttribute( "parameter2", effect.EffectParameters[1] );
						if ( effect.EffectParameters.Count == 3 )
							effectNode.AddAttribute( "parameter3", effect.EffectParameters[2] );


						effectsNode.AppendChild( effectNode );
					}
				}
			}
			newsNode.AppendChild( effectsNode );

			{
				XmlNode dataNode = doc.CreateElement( "data" );

				dataNode.AddAttribute( "genre", ((int)news.Genre).ToString() );
				dataNode.AddAttribute( "price", news.Price.ToString() );
				dataNode.AddAttribute( "quality", news.Quality.ToString() );

				newsNode.AppendChild( dataNode );
			}

			//{
			//XmlNode dataBehavior = doc.CreateElement( "behavior" );

			//dataBehavior.AddAttribute( "type", ((int)news.NewsType).ToString() );
			//    //dataBehavior.AddAttribute("handling", ((int)news.NewsHandling).ToString());


			//    //dataBehavior.AddAttribute("Resource_type_1", news.Resource1Type != null ? news.Resource1Type.ToString() : null);
			//    //dataBehavior.AddAttribute("Resource_type_2", news.Resource2Type != null ? news.Resource2Type.ToString() : null);
			//    //dataBehavior.AddAttribute("Resource_type_3", news.Resource3Type != null ? news.Resource3Type.ToString() : null);
			//    //dataBehavior.AddAttribute("Resource_type_4", news.Resource4Type != null ? news.Resource4Type.ToString() : null);

			//    adNode.AppendChild( dataBehavior );
			//}

			{
				XmlNode dataCond = doc.CreateElement( "conditions" );

				//dataCond.AddAttribute("available_after_days", news.AvailableAfterDays.ToString());
				dataCond.AddAttribute( "year_range_from", news.YearRangeFrom.ToString() );
				dataCond.AddAttribute( "year_range_to", news.YearRangeTo.ToString() );
				//dataCond.AddAttribute("min_hours_after_predecessor", news.MinHoursAfterPredecessor.ToString());
				//dataCond.AddAttribute("time_range_from", news.TimeRangeFrom.ToString());
				//dataCond.AddAttribute("time_range_to", news.TimeRangeTo.ToString());

				newsNode.AppendChild( dataCond );
			}

			return newsNode;
		}


		public XmlNode SetInsignificantPersonDetailNode( XmlDocument doc, XmlElement element, TVTPerson person, DatabaseVersion dbVersion, DataStructure dataStructure )
		{
			var personNode = doc.CreateElement( "person" );
			{
				personNode.AddAttribute( "id", person.Id.ToString() );

				if ( person.DataType == TVTDataType.Fictitious )
					dataStructure = DataStructure.OriginalData;

				switch ( dataStructure )
				{
					case DataStructure.FakeData:
						if ( !string.IsNullOrEmpty( person.FakeFirstName ) )
							personNode.AddAttribute( "first_name", person.FakeFirstName );
						else
							personNode.AddAttribute( "first_name", person.FirstName );
						personNode.AddAttribute( "last_name", person.FakeLastName );
						personNode.AddAttribute( "nick_name", person.FakeNickName );
						break;
					case DataStructure.OriginalData:
						personNode.AddAttribute( "first_name", person.FirstName );
						personNode.AddAttribute( "last_name", person.LastName );
						personNode.AddAttribute( "nick_name", person.NickName );
						break;
				}
			}
			element.AppendChild( personNode );
			return personNode;
		}

		public XmlNode SetCelebrityPersonDetailNode( XmlDocument doc, XmlElement element, TVTPerson person, DatabaseVersion dbVersion, DataStructure dataStructure )
		{
			var personNode = doc.CreateElement( "person" );
			{
				personNode.AddAttribute( "id", person.Id.ToString() );
				personNode.AddAttribute( "tmdb_id", person.TmdbId.ToString() );
				personNode.AddAttribute( "imdb_id", person.ImdbId != null ? person.ImdbId.ToString() : null );
				personNode.AddAttribute( "creator", person.CreatorId );
                personNode.AddAttribute( "lastmodified", person.LastModified != null ? person.LastModified.ToString() : "" );
			}
			element.AppendChild( personNode );

			if ( person.DataType == TVTDataType.Fictitious )
				dataStructure = DataStructure.OriginalData;

			switch ( dataStructure )
			{
				case DataStructure.FakeData:
					if ( person.DataType == TVTDataType.Fictitious || string.IsNullOrEmpty( person.FakeFirstName ) )
						personNode.AddElement( "first_name", person.FirstName );
					else
						personNode.AddElement( "first_name", person.FakeFirstName );

					if ( person.DataType != TVTDataType.Fictitious )
						personNode.AddElement( "last_name", person.FakeLastName );
					else
						personNode.AddElement( "last_name", person.LastName );

					if ( person.DataType != TVTDataType.Fictitious )
						personNode.AddElement( "nick_name", person.FakeNickName );
					else
						personNode.AddElement( "nick_name", person.NickName );
					break;
				case DataStructure.OriginalData:
					personNode.AddElement( "first_name", person.FirstName );
					personNode.AddElement( "last_name", person.LastName );
					personNode.AddElement( "nick_name", person.NickName );
					break;
			}

			{
				XmlNode imagesNode = doc.CreateElement( "images" );
				if ( !string.IsNullOrEmpty( person.ImageUrl ) )
				{
					imagesNode.AddElement( "image", person.ImageUrl );
				}
				personNode.AppendChild( imagesNode );
			}

			var functionsNode = doc.CreateElement( "functions" );
			{
				if ( person.Functions != null )
				{
					foreach ( var func in person.Functions )
					{
						functionsNode.AddElement( "function", ((int)func).ToString() );
					}
				}
			}
			personNode.AppendChild( functionsNode );

			{
				XmlNode detailsNode = doc.CreateElement( "details" );

				detailsNode.AddAttribute( "gender", ((int)person.Gender).ToString() );
                detailsNode.AddAttribute( "birthday", person.Birthday.HasValue ? person.Birthday.Value.ToString() : "" );
                detailsNode.AddAttribute( "deathday", person.Deathday.HasValue ? person.Deathday.Value.ToString() : "" );
				detailsNode.AddAttribute( "country", person.Country );

				personNode.AppendChild( detailsNode );
			}

			{
				XmlNode dataNode = doc.CreateElement( "data" );
				dataNode.AddAttribute( "prominence", person.Prominence.ToString() );
				dataNode.AddAttribute( "skill", person.Skill.ToString() );
				dataNode.AddAttribute( "fame", person.Fame.ToString() );
				dataNode.AddAttribute( "scandalizing", person.Scandalizing.ToString() );
				dataNode.AddAttribute( "price_mod", person.PriceMod.ToString( CultureInfo.InvariantCulture ) );

				dataNode.AddAttribute( "power", person.Power.ToString() );
				dataNode.AddAttribute( "humor", person.Humor.ToString() );
				dataNode.AddAttribute( "charisma", person.Charisma.ToString() );
				dataNode.AddAttribute( "appearance", person.Appearance.ToString() );

				dataNode.AddAttribute( "topgenre1", ((int)person.TopGenre1).ToString() );
				dataNode.AddAttribute( "topgenre2", ((int)person.TopGenre2).ToString() );
				personNode.AppendChild( dataNode );
			}

			return personNode;
		}

		public XmlNode SetProgrammeDetailNode( XmlDocument doc, XmlElement element, TVTProgramme programme, DatabaseVersion dbVersion, DataStructure dataStructure )
		{
			XmlNode movieNode, dataNode;

			//if ( programme.ProgrammeType == TVTProgrammeType.Series )
			//    throw new NotImplementedException();

			movieNode = doc.CreateElement( "programme" );
			{
				movieNode.AddAttribute( "id", programme.Id.ToString() );
				//movieNode.AddAttribute("status", programme.DataStatus.ToString());
				movieNode.AddAttribute( "product", ((int)programme.ProductType).ToString() );
				movieNode.AddAttribute( "type", ((int)programme.ProgrammeType).ToString() );
				movieNode.AddAttribute( "tmdb_id", programme.TmdbId.ToString() );
				movieNode.AddAttribute( "imdb_id", programme.ImdbId );
				movieNode.AddAttribute( "rt_id", programme.RottenTomatoesId.HasValue ? programme.RottenTomatoesId.Value.ToString() : "" );
				movieNode.AddAttribute( "creator", programme.CreatorId );
			}
			element.AppendChild( movieNode );

			SetTitleAndDescription( doc, movieNode, dataStructure, programme );

			//movieNode.AddElement( "version", movie.DataVersion.ToString() );

			{
				XmlNode imagesNode = doc.CreateElement( "images" );
				if ( !string.IsNullOrEmpty( programme.ImageUrl ) )
				{
					imagesNode.AddElement( "image", programme.ImageUrl );
				}
				if ( !NodeIsEmpty( imagesNode ) )
					movieNode.AppendChild( imagesNode );
			}

			SetStaffNode( doc, movieNode, programme );

			var groupNode = doc.CreateElement( "groups" );
			{
				groupNode.AddAttributeC( "target_groups", programme.TargetGroups.Sum( x => (int)x ).ToString() );
				groupNode.AddAttributeC( "pro_pressure_groups", programme.ProPressureGroups.Sum( x => (int)x ).ToString() );
				groupNode.AddAttributeC( "contra_pressure_groups", programme.ContraPressureGroups.Sum( x => (int)x ).ToString() );
			}
			if ( !NodeIsEmpty( groupNode ) )
				movieNode.AppendChild( groupNode );

			int flagSum = 0;

			if ( programme.Flags != null )
			{
				flagSum = programme.Flags.Sum( x => (int)x );
			}

			dataNode = doc.CreateElement( "data" );
			{
				dataNode.AddAttributeC( "country", programme.Country );
				dataNode.AddAttributeC( "year", programme.Year.ToString() );
				dataNode.AddAttributeC( "distribution", ((int)programme.DistributionChannel).ToString() );

				dataNode.AddAttributeC( "maingenre", ((int)programme.MainGenre).ToString() );
				dataNode.AddAttributeC( "subgenre", ((int)programme.SubGenre).ToString() );

				dataNode.AddAttributeC( "flags", flagSum.ToString() );
				dataNode.AddAttributeC( "blocks", programme.Blocks.ToString() );
				dataNode.AddAttributeC( "time", programme.LiveHour.ToString() );
				dataNode.AddAttributeC( "price_mod", programme.PriceMod.ToString( CultureInfo.InvariantCulture ) );
			}
			if ( !NodeIsEmpty( dataNode ) )
				movieNode.AppendChild( dataNode );

			var ratingsNode = doc.CreateElement( "ratings" );
			{
				ratingsNode.AddAttributeC( "critics", programme.CriticsRate.ToString() );
				ratingsNode.AddAttributeC( "speed", programme.ViewersRate.ToString() );
				ratingsNode.AddAttributeC( "outcome", programme.BoxOfficeRate.ToString() );
			}
			if ( !NodeIsEmpty( ratingsNode ) )
				movieNode.AppendChild( ratingsNode );

			var childrenNode = doc.CreateElement( "children" );
			{
				if ( programme.Children != null )
				{
					foreach ( var episode in programme.Children.OrderBy( x => x.EpisodeIndex ) )
					{
						SetProgrammeDetailNode( doc, childrenNode, episode, dbVersion, dataStructure );

						//episodesNode.AddElement( "episode", episode.Id.ToString() );
					}
				}
			}
			if ( !NodeIsEmpty( childrenNode ) )
				movieNode.AppendChild( childrenNode );

			return movieNode;
		}

		private void SetStaffNode( XmlDocument doc, XmlNode parentNode, ITVTProgrammeCore programmeCore )
		{
			var staffNode = doc.CreateElement( "staff" );
			{
				foreach ( var staffMember in programmeCore.Staff.OrderBy( x => x.SortIndex() ) )
				{
					var memberNode = doc.CreateElement( "member" );
					memberNode.AddAttribute( "index", staffMember.Index.ToString() );
					memberNode.AddAttribute( "function", ((int)staffMember.Function).ToString() );
					memberNode.InnerText = staffMember.Person.Id.ToString();
					staffNode.AppendChild( memberNode );
				}
			}

			if ( !NodeIsEmpty( staffNode ) )
				parentNode.AppendChild( staffNode );
		}

		private void SetTitleAndDescriptionMinimal( XmlDocument doc, XmlNode node, DataStructure dataStructure, ITVTNamesBasic nameObject )
		{
			var titleNode = doc.CreateElement( "title" );
			var descriptionNode = doc.CreateElement( "description" );
			{
				titleNode.AddElement( "de", nameObject.TitleDE );
				titleNode.AddElement( "en", nameObject.TitleEN );
				descriptionNode.AddElement( "de", nameObject.DescriptionDE );
				descriptionNode.AddElement( "en", nameObject.DescriptionEN );
			}
			node.AppendChild( titleNode );
			node.AppendChild( descriptionNode );
		}

		private void SetTitleAndDescription( XmlDocument doc, XmlNode node, DataStructure dataStructure, ITVTNames nameObject )
		{
			if ( nameObject.DataType == TVTDataType.Fictitious )
				dataStructure = DataStructure.OriginalData;

			switch ( dataStructure )
			{
				case DataStructure.FakeData:
					var titleNode = doc.CreateElement( "title" );
					var descriptionNode = doc.CreateElement( "description" );
					{
						if ( !string.IsNullOrEmpty( nameObject.FakeTitleDE ) )
							titleNode.AddElement( "de", nameObject.FakeTitleDE );
						else
							titleNode.AddElement( "de", "NEED_FAKE: " + nameObject.TitleDE );

						if ( !string.IsNullOrEmpty( nameObject.FakeTitleDE ) )
							titleNode.AddElement( "en", nameObject.FakeTitleEN );
						else
							titleNode.AddElement( "en", "NEED_FAKE: " + nameObject.TitleEN );

						if ( !string.IsNullOrEmpty( nameObject.FakeDescriptionDE ) )
							descriptionNode.AddElement( "de", nameObject.FakeDescriptionDE );
						else
							descriptionNode.AddElement( "de", nameObject.DescriptionDE );

						if ( !string.IsNullOrEmpty( nameObject.FakeDescriptionEN ) )
							descriptionNode.AddElement( "en", nameObject.FakeDescriptionEN );
						else
							descriptionNode.AddElement( "en", nameObject.DescriptionEN );
					}
					node.AppendChild( titleNode );
					node.AppendChild( descriptionNode );
					break;
				case DataStructure.OriginalData:
					SetTitleAndDescriptionMinimal( doc, node, dataStructure, nameObject );
					break;
			}
		}

		private void SetTitleAndDescriptionBasic( XmlDocument doc, XmlNode node, ITVTNamesBasic nameObject )
		{

			var titleNode = doc.CreateElement( "title" );
			var descriptionNode = doc.CreateElement( "description" );
			{
				titleNode.AddElement( "de", nameObject.TitleDE );
				titleNode.AddElement( "en", nameObject.TitleEN );
				descriptionNode.AddElement( "de", nameObject.DescriptionDE );
				descriptionNode.AddElement( "en", nameObject.DescriptionEN );
			}
			node.AppendChild( titleNode );
			node.AppendChild( descriptionNode );
		}

		//public XmlNode SetEpisodeDetailNode( XmlDocument doc, XmlElement element, TVTEpisode episode, DatabaseVersion dbVersion, DataStructure dataStructure )
		//{
		//    XmlNode episodeNode;

		//    episodeNode = doc.CreateElement( "episode" );
		//    {
		//        episodeNode.AddAttribute( "id", episode.Id.ToString() );
		//        episodeNode.AddAttribute( "index", episode.EpisodeIndex.ToString() );
		//    }
		//    element.AppendChild( episodeNode );

		//    SetTitleAndDescription( doc, episodeNode, dataStructure, episode );

		//    SetStaffNode( doc, episodeNode, episode );

		//    var ratingsNode = doc.CreateElement( "ratings" );
		//    {
		//        ratingsNode.AddAttribute( "critics", episode.CriticsRate.ToString() );
		//        ratingsNode.AddAttribute( "speed", episode.ViewersRate.ToString() );
		//    }
		//    episodeNode.AppendChild( ratingsNode );

		//    return episodeNode;
		//}

		public bool NodeIsEmpty( XmlNode node )
		{
			return (node.Attributes.Count == 0 && !node.HasChildNodes);
		}

        public TVTPerson LoadPerson( XmlNode xmlNode, DataStructure dataStructure )
        {
            var result = new TVTPerson();
            result.Id = xmlNode.GetAttribute( "id" );
            result.TmdbId = xmlNode.GetAttributeInteger( "tmdb_id" );
            result.ImdbId = xmlNode.GetAttribute( "imdb_id" );
            result.CreatorId = xmlNode.GetAttribute( "creator" );
            var lastMod = xmlNode.GetAttribute( "lastmodified" );
            DateTime lastModified;
            if ( DateTime.TryParse( lastMod, out lastModified ) )
                result.LastModified = lastModified;

			foreach ( XmlLinkedNode movieChild in xmlNode.ChildNodes )
			{
				switch ( movieChild.Name )
				{
					case "first_name":
                        if ( dataStructure == DataStructure.FakeData )
                            result.FakeFirstName = movieChild.GetElementValue();
                        else
                            result.FirstName = movieChild.GetElementValue();
						break;
					case "last_name":
                        if ( dataStructure == DataStructure.FakeData )
                            result.FakeLastName = movieChild.GetElementValue();
                        else
                            result.LastName = movieChild.GetElementValue();
						break;
					case "nick_name":
                        if ( dataStructure == DataStructure.FakeData )
                            result.FakeNickName = movieChild.GetElementValue();
                        else
                            result.NickName = movieChild.GetElementValue();
						break;
					case "images":
						foreach ( XmlLinkedNode tileNode in movieChild.ChildNodes )
						{
							switch ( tileNode.Name )
							{
								case "image":
									result.ImageUrl = tileNode.GetElementValue();
									break;
							}
						}
						break;
					case "functions":
						foreach ( XmlLinkedNode tileNode in movieChild.ChildNodes )
						{
							switch ( tileNode.Name )
							{
								case "function":
									result.Functions.Add((TVTPersonFunction)tileNode.GetElementValueInteger());
									break;
							}
						}
						break;
					case "details":
                        result.Gender = (TVTPersonGender)movieChild.GetAttributeInteger( "gender" );
                        result.Birthday = movieChild.GetAttributeNullableInteger( "birthday" );
                        result.Deathday = movieChild.GetAttributeNullableInteger( "deathday" );
                        result.Country = movieChild.GetAttribute( "country" );
						break;
					case "data":
                        result.Prominence = movieChild.GetAttributeInteger( "prominence" );
                        result.Skill = movieChild.GetAttributeInteger( "skill" );
                        result.Fame = movieChild.GetAttributeInteger( "fame" );
                        result.Scandalizing = movieChild.GetAttributeInteger( "scandalizing" );
                        result.PriceMod = movieChild.GetAttributeInteger( "price_mod" );
                        result.Power = movieChild.GetAttributeInteger( "power" );
                        result.Humor = movieChild.GetAttributeInteger( "humor" );
                        result.Charisma = movieChild.GetAttributeInteger( "charisma" );
                        result.Appearance = movieChild.GetAttributeInteger( "appearance" );
                        var topGenre1 = movieChild.GetAttributeNullableInteger( "topgenre1" );
                        var topGenre2 = movieChild.GetAttributeNullableInteger( "topgenre2" );
                        result.TopGenre1 = topGenre1.HasValue ? (TVTProgrammeGenre)topGenre1 : TVTProgrammeGenre.Undefined;
                        result.TopGenre2 = topGenre1.HasValue ? (TVTProgrammeGenre)topGenre2 : TVTProgrammeGenre.Undefined;
						break;
                }
            }

            return result;
        }

        public TVTProgramme LoadProgramme( XmlNode xmlNode, bool isFake )
        {
            var result = new TVTProgramme();
            result.Id = xmlNode.GetAttribute( "id" );
            result.CreatorId = xmlNode.GetAttribute( "creator" );
            var lastMod = xmlNode.GetAttribute( "lastmodified" );
            DateTime lastModified;
            if ( DateTime.TryParse( lastMod, out lastModified ) )
                result.LastModified = lastModified;

            throw new NotImplementedException();

            //foreach ( XmlLinkedNode movieChild in xmlNode.ChildNodes )
            //{
            //    switch ( movieChild.Name )
            //    {
            //        case "title":
            //            foreach ( XmlLinkedNode tileNode in movieChild.ChildNodes )
            //            {
            //                switch ( tileNode.Name )
            //                {
            //                    case "de":
            //                        result.TitleDE = tileNode.GetElementValue();
            //                        break;
            //                    case "en":
            //                        result.TitleEN = tileNode.GetElementValue();
            //                        break;
            //                }
            //            }
            //            break;
            //        case "description":
            //            foreach ( XmlLinkedNode descNode in movieChild.ChildNodes )
            //            {
            //                switch ( descNode.Name )
            //                {
            //                    case "de":
            //                        result.DescriptionDE = descNode.GetElementValue();
            //                        break;
            //                    case "en":
            //                        result.DescriptionEN = descNode.GetElementValue();
            //                        break;
            //                }
            //            }
            //            break;
            //        case "restrictions":
            //            result.ValidFrom = movieChild.GetAttributeInteger( "valid_from" );
            //            result.ValidTill = movieChild.GetAttributeInteger( "valid_till" );
            //            break;
            //        case "conditions":
            //            result.MinAudience = movieChild.GetAttributeFloat( "min_audience" );
            //            result.MinImage = movieChild.GetAttributeInteger( "min_image" );
            //            result.TargetGroup = (TVTTargetGroup)movieChild.GetAttributeInteger( "target_group" );

            //            foreach ( XmlLinkedNode cond in movieChild.ChildNodes )
            //            {
            //                switch ( cond.Name )
            //                {
            //                    case "allowed_genre":
            //                        if ( result.AllowedGenres == null )
            //                            result.AllowedGenres = new List<TVTProgrammeGenre>();
            //                        result.AllowedGenres.Add( (TVTProgrammeGenre)Enum.Parse( typeof( TVTProgrammeGenre ), cond.GetElementValue() ) );
            //                        break;
            //                    case "prohibited_genre":
            //                        if ( result.ProhibitedGenres == null )
            //                            result.ProhibitedGenres = new List<TVTProgrammeGenre>();
            //                        result.ProhibitedGenres.Add( (TVTProgrammeGenre)Enum.Parse( typeof( TVTProgrammeGenre ), cond.GetElementValue() ) );
            //                        break;
            //                    case "allowed_programme_type":
            //                        if ( result.AllowedProgrammeTypes == null )
            //                            result.AllowedProgrammeTypes = new List<TVTProgrammeType>();
            //                        result.AllowedProgrammeTypes.Add( (TVTProgrammeType)Enum.Parse( typeof( TVTProgrammeType ), cond.GetElementValue() ) );
            //                        break;
            //                    case "prohibited_programme_type":
            //                        if ( result.ProhibitedProgrammeTypes == null )
            //                            result.ProhibitedProgrammeTypes = new List<TVTProgrammeType>();
            //                        result.ProhibitedProgrammeTypes.Add( (TVTProgrammeType)Enum.Parse( typeof( TVTProgrammeType ), cond.GetElementValue() ) );
            //                        break;
            //                }
            //            }

            //            //AllowedGenres

            //            break;
            //        case "data":
            //            //result.Infomercial = movieChild.GetAttributeInteger( "infomercial" ) == 1;
            //            result.Quality = movieChild.GetAttributeInteger( "quality" );
            //            result.Repetitions = movieChild.GetAttributeInteger( "repetitions" );
            //            result.Duration = movieChild.GetAttributeInteger( "duration" );
            //            result.FixPrice = movieChild.GetAttributeInteger( "fix_price" ) == 1;
            //            result.Profit = movieChild.GetAttributeInteger( "profit" );
            //            result.Penalty = movieChild.GetAttributeInteger( "penalty" );

            //            result.ProPressureGroups = EnumFlag<TVTPressureGroup>.New( movieChild.GetAttributeInteger( "pro_pressure_groups" ) ).ToTypeList();
            //            result.ContraPressureGroups = EnumFlag<TVTPressureGroup>.New( movieChild.GetAttributeInteger( "contra_pressure_groups" ) ).ToTypeList();
            //            break;
            //    }
            //}

            return result;
        }

		public TVTAdvertising LoadAd( XmlNode xmlNode, bool isFake )
		{
			var result = new TVTAdvertising();
			result.Id = xmlNode.GetAttribute( "id" );
			result.CreatorId = xmlNode.GetAttribute( "creator" );
			var lastMod = xmlNode.GetAttribute( "lastmodified" );
			DateTime lastModified;
			if ( DateTime.TryParse( lastMod, out lastModified ) )
				result.LastModified = lastModified;
			

			foreach ( XmlLinkedNode movieChild in xmlNode.ChildNodes )
			{
				switch ( movieChild.Name )
				{
					case "title":
						foreach ( XmlLinkedNode tileNode in movieChild.ChildNodes )
						{
							switch ( tileNode.Name )
							{
								case "de":
									result.TitleDE = tileNode.GetElementValue();
									break;
								case "en":
									result.TitleEN = tileNode.GetElementValue();
									break;
							}
						}
						break;
					case "description":
						foreach ( XmlLinkedNode descNode in movieChild.ChildNodes )
						{
							switch ( descNode.Name )
							{
								case "de":
									result.DescriptionDE = descNode.GetElementValue();
									break;
								case "en":
									result.DescriptionEN = descNode.GetElementValue();
									break;
							}
						}
						break;
					case "restrictions":
						result.ValidFrom = movieChild.GetAttributeInteger( "valid_from" );
						result.ValidTill = movieChild.GetAttributeInteger( "valid_till" );
						break;
					case "conditions":
						result.MinAudience = movieChild.GetAttributeFloat( "min_audience" );
						result.MinImage = movieChild.GetAttributeInteger( "min_image" );
						result.TargetGroup = (TVTTargetGroup)movieChild.GetAttributeInteger( "target_group" );

						foreach ( XmlLinkedNode cond in movieChild.ChildNodes )
						{
							switch ( cond.Name )
							{
								case "allowed_genre":
                                    if ( result.AllowedGenres == null )
                                        result.AllowedGenres = new List<TVTProgrammeGenre>();
									result.AllowedGenres.Add( (TVTProgrammeGenre)Enum.Parse( typeof( TVTProgrammeGenre ), cond.GetElementValue() ) );
									break;
								case "prohibited_genre":
                                    if ( result.ProhibitedGenres == null )
                                        result.ProhibitedGenres = new List<TVTProgrammeGenre>();
									result.ProhibitedGenres.Add( (TVTProgrammeGenre)Enum.Parse( typeof( TVTProgrammeGenre ), cond.GetElementValue() ) );
									break;
								case "allowed_programme_type":
                                    if ( result.AllowedProgrammeTypes == null )
                                        result.AllowedProgrammeTypes = new List<TVTProgrammeType>();
									result.AllowedProgrammeTypes.Add( (TVTProgrammeType)Enum.Parse( typeof( TVTProgrammeType ), cond.GetElementValue() ) );
									break;
								case "prohibited_programme_type":
                                    if ( result.ProhibitedProgrammeTypes == null )
                                        result.ProhibitedProgrammeTypes = new List<TVTProgrammeType>();
									result.ProhibitedProgrammeTypes.Add( (TVTProgrammeType)Enum.Parse( typeof( TVTProgrammeType ), cond.GetElementValue() ) );
									break;
							}
						}

						//AllowedGenres

						break;
					case "data":
						//result.Infomercial = movieChild.GetAttributeInteger( "infomercial" ) == 1;
						result.Quality = movieChild.GetAttributeInteger( "quality" );
						result.Repetitions = movieChild.GetAttributeInteger( "repetitions" );
						result.Duration = movieChild.GetAttributeInteger( "duration" );
						result.FixPrice = movieChild.GetAttributeInteger( "fix_price" ) == 1;
						result.Profit = movieChild.GetAttributeInteger( "profit" );
						result.Penalty = movieChild.GetAttributeInteger( "penalty" );

                        result.ProPressureGroups = EnumFlag<TVTPressureGroup>.New( movieChild.GetAttributeInteger( "pro_pressure_groups" ) ).ToTypeList();
						result.ContraPressureGroups = EnumFlag<TVTPressureGroup>.New( movieChild.GetAttributeInteger( "contra_pressure_groups" ) ).ToTypeList();
						break;
				}
			}

			return result;
		}

        public ITVTDatabase LoadXML( string filename, ITVTDatabase database, DataStructure dataStructure )
		{
			var result = database;
			var doc = new XmlDocument();
			doc.Load( filename );

			var versionElement = doc.GetElementsByTagName( "version" );
			if ( versionElement[0].HasAttribute( "value" ) )
			{
				var version = versionElement[0].GetAttributeInteger( "value" );
				if ( version != 3 )
					throw new NotSupportedException( "Only database version '3' is supported." );
			}

            {
                var programmes = new List<TVTProgramme>();
                var allNews = doc.GetElementsByTagName( "allprogrammes" );

                foreach ( XmlNode xmlNews in allNews )
                {
                    foreach ( XmlNode childNode in xmlNews.ChildNodes )
                    {
                        switch ( childNode.Name )
                        {
                            case "programme":
                                programmes.Add( LoadProgramme( childNode, true ) );
                                break;
                            default:
                                throw new NotSupportedException( "Only 'news'-tags are supported." );
                        }
                    }
                }
                database.AddProgrammes( programmes );
            }

            {
                var people = new List<TVTPerson>();
                var celebritypeople = doc.GetElementsByTagName( "celebritypeople" );

                foreach ( XmlNode xmlPerson in celebritypeople )
                {
                    foreach ( XmlNode childNode in xmlPerson.ChildNodes )
                    {
                        switch ( childNode.Name )
                        {
                            case "person":
                                people.Add( LoadPerson( childNode, dataStructure ) );
                                break;
                            default:
                                throw new NotSupportedException( "Only 'programme'-tags are supported." );
                        }
                    }
                }
                database.AddPeople( people );
            }

			//{
			//    var series = new List<MovieOldV2>();
			//    var allSeries = doc.GetElementsByTagName( "allseries" );

			//    foreach ( XmlNode xmlSeries in allSeries )
			//    {
			//        foreach ( XmlNode childNode in xmlSeries.ChildNodes )
			//        {
			//            switch ( childNode.Name )
			//            {
			//                case "serie":
			//                    series.AddRange( LoadMovie( childNode, true ) );
			//                    break;
			//                default:
			//                    throw new NotSupportedException( "Only 'serie'-tags are supported." );
			//            }
			//        }
			//    }
			//    OldV2Converter.Convert( series, database, dataRoot );
			//}

			//{
			//    var news = new List<NewsOldV2>();
			//    var allNews = doc.GetElementsByTagName( "allnews" );

			//    foreach ( XmlNode xmlNews in allNews )
			//    {
			//        foreach ( XmlNode childNode in xmlNews.ChildNodes )
			//        {
			//            switch ( childNode.Name )
			//            {
			//                case "news":
			//                    news.AddRange( LoadNews( childNode, true ) );
			//                    break;
			//                default:
			//                    throw new NotSupportedException( "Only 'news'-tags are supported." );
			//            }
			//        }
			//    }
			//    OldV2Converter.Convert( news, database, dataRoot );
			//}

			{
				var ads = new List<TVTAdvertising>();
				var allNews = doc.GetElementsByTagName( "allads" );

				foreach ( XmlNode xmlNews in allNews )
				{
					foreach ( XmlNode childNode in xmlNews.ChildNodes )
					{
						switch ( childNode.Name )
						{
							case "ad":
								ads.Add( LoadAd( childNode, true ) );
								break;
							default:
								throw new NotSupportedException( "Only 'news'-tags are supported." );
						}
					}
				}
				database.AddAdvertisings( ads );
			}

			database.RefreshReferences();

			return result;
		}

		public ITVTDatabase LoadXMLV3Beta( string filename, ITVTDatabase database )
		{
			var result = database;
			var doc = new XmlDocument();
			doc.Load( filename );

			var versionElement = doc.GetElementsByTagName( "version" );
			if ( versionElement[0].HasAttribute( "value" ) )
			{
				var version = versionElement[0].GetAttributeInteger( "value" );
				if ( version != 3 )
					throw new NotSupportedException( "Only database version '3' is supported." );
			}

			//{
			//    var movies = new List<TVTProgramme>();
			//    var allprogrammes = doc.GetElementsByTagName( "allprogrammes" );

			//    foreach ( XmlNode xmlProgramme in allprogrammes )
			//    {
			//        foreach ( XmlNode childNode in xmlProgramme.ChildNodes )
			//        {
			//            switch ( childNode.Name )
			//            {
			//                case "programme":
			//                    movies.AddRange( LoadMovie( childNode, true ) );
			//                    break;
			//                default:
			//                    throw new NotSupportedException( "Only 'programme'-tags are supported." );
			//            }
			//        }
			//    }
			//    OldV2Converter.Convert( movies, database, dataRoot );
			//}

			//{
			//    var series = new List<MovieOldV2>();
			//    var allSeries = doc.GetElementsByTagName( "allseries" );

			//    foreach ( XmlNode xmlSeries in allSeries )
			//    {
			//        foreach ( XmlNode childNode in xmlSeries.ChildNodes )
			//        {
			//            switch ( childNode.Name )
			//            {
			//                case "serie":
			//                    series.AddRange( LoadMovie( childNode, true ) );
			//                    break;
			//                default:
			//                    throw new NotSupportedException( "Only 'serie'-tags are supported." );
			//            }
			//        }
			//    }
			//    OldV2Converter.Convert( series, database, dataRoot );
			//}

			//{
			//    var news = new List<NewsOldV2>();
			//    var allNews = doc.GetElementsByTagName( "allnews" );

			//    foreach ( XmlNode xmlNews in allNews )
			//    {
			//        foreach ( XmlNode childNode in xmlNews.ChildNodes )
			//        {
			//            switch ( childNode.Name )
			//            {
			//                case "news":
			//                    news.AddRange( LoadNews( childNode, true ) );
			//                    break;
			//                default:
			//                    throw new NotSupportedException( "Only 'news'-tags are supported." );
			//            }
			//        }
			//    }
			//    OldV2Converter.Convert( news, database, dataRoot );
			//}

			{
				var ads = new List<TVTAdvertising>();
				var allNews = doc.GetElementsByTagName( "allads" );

				foreach ( XmlNode xmlNews in allNews )
				{
					foreach ( XmlNode childNode in xmlNews.ChildNodes )
					{
						switch ( childNode.Name )
						{
							case "ad":
								var ad = LoadAd( childNode, true );
								//Es gab eine Zwischenversion mit Problemen bei der TargetGroup
								ads.Add( ad );
								break;
							default:
								throw new NotSupportedException( "Only 'news'-tags are supported." );
						}
					}
				}
				database.AddAdvertisings( ads );
			}

			database.RefreshReferences();

			return result;
		}
	}
}
