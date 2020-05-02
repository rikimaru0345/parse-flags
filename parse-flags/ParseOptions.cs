using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFlags
{
	public class ParseOptions
	{
		// - MatchByAttribute: Disabled, Exact, CaseInsensitive
		// - MatchByPropertyName: Disabled, Exact, CaseInsensitive
		public bool AllowCaseInsensitiveMatching { get; set; } = false;

		// - 
	}
}
