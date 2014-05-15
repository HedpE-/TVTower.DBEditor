﻿using System.Collections.Generic;

namespace TVTower.Entities
{
	public class TVTPerson : TVTEntity
	{
		public string FullName { get { return string.Format( "{0} {1}", FirstName, LastName ); } }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string NickName { get; set; }

		public string FakeFullName { get { return string.Format( "{0} {1}", FakeFirstName, FakeLastName ); } }
		public string FakeFirstName { get; set; }
		public string FakeLastName { get; set; }
		public string FakeNickName { get; set; }

		public int TmdbId { get; set; }
		public string ImdbId { get; set; }
		public string ImageUrl { get; set; }

		public List<TVTPersonFunction> Functions { get; set; }

		public TVTPersonGender Gender { get; set; } //Als Enum?		
		public string Birthday { get; set; }
		public string Deathday { get; set; }
		public string PlaceOfBirth { get; set; }
		public string Country { get; set; }


		public int ProfessionSkill { get; set; }		//0 - 100	Für Regisseur, Musiker und Intellektueller: Wie gut kann er sein Handwerk	
		public int Fame { get; set; }					//0 - 100	Kinokasse ++							Wie berühmt ist die Person?
		public int Scandalizing { get; set; }			//0 - 100	Besonders Interessant für Shows und Sonderevents
		public int PriceFactor { get; set; }

		public int Power { get; set; }					//0 - 100	Kinokasse +		Tempo +++		Bonus bei manchen Genre (wie Action)
		public int Humor { get; set; }					//0 - 100	Kinokasse +		Tempo ++		Bonus bei manchen Genre (wie Komödie)
		public int Charisma { get; set; }				//0 - 100	Kinokasse +		Kritik ++		Bonus bei manchen Genre (wie Liebe, Drama, Komödie)
		public int EroticAura { get; set; }				//0 - 100	Kinokasse +++ 	Tempo +			Bonus bei manchen Genre (wie Erotik, Liebe, Action)
		public int CharacterSkill { get; set; }			//0 - 100	Kinokasse +		Kritik +++		Bonus bei manchen Genre (wie Drama)

		public TVTMovieGenre TopMovieGenre { get; set; }
		public TVTEventGenre TopEventGenre { get; set; }
		public TVTReportageGenre TopReportageGenre { get; set; }
		public TVTShowGenre TopShowGenre { get; set; }

		public TVTPerson()
		{
			Functions = new List<TVTPersonFunction>();
		}
	}
}
