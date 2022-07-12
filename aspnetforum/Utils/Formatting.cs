using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
using Jitbit.Utils;

namespace aspnetforum.Utils
{
	public static class Formatting
	{
		public static string FormatSignature(string signature)
		{
			signature = signature.Trim();
			if (signature.Length == 0) return string.Empty;
			else
			{
				return "<br/><br/>--<br/>" + FormatMessageHTML(signature);
			}
		}

		public static string StripBBCode(string input)
		{
			return StringUtils.StripBBCode(input);
		}

		/// <summary>
		/// removes everything outside the <body> tag, <script> tags and all betwwen 'em
		/// removes all html tags at all
		/// </summary>
		/// <param name="html"></param>
		/// <returns></returns>
		public static string StripHTML(string html)
		{
			return StringUtils.StripHTML(html);
		}

		/// <summary>
		/// converts bbcode to HTML
		/// </summary>
		public static string FormatMessageHTML(string input)
		{
			if (input.Length == 0) return string.Empty;

			string msg = input;

			//youtube (to prevent standard bbencoding)
			Regex regexYouTube = new Regex(@"(\s|^)(http://www\.youtube\.com/watch\?v=([^\s^&]+)[^\s]*)", RegexOptions.IgnoreCase);
			msg = regexYouTube.Replace(msg, "$1[youtube]$3[/youtube]");

			msg = StringUtils.BBCode2HTML(msg, Resources.various.Wrote);

			//youtube AGAIN
			regexYouTube = new Regex(@"\[youtube\]([^\]]+)\[/youtube\]", RegexOptions.IgnoreCase);
			msg = regexYouTube.Replace(msg, "<object width=\"425\" height=\"344\"><param name=\"movie\" value=\"http://www.youtube.com/v/$1\"></param><param name=\"allowFullScreen\" value=\"true\"></param><param name=\"allowscriptaccess\" value=\"always\"></param><embed src=\"http://www.youtube.com/v/$1\" type=\"application/x-shockwave-flash\" allowscriptaccess=\"always\" allowfullscreen=\"true\" width=\"425\" height=\"344\"></embed></object>");
			
			//smilies
			if (Settings.AllowSmilies)
				msg = FormatSmilies(msg);

			return msg;
		}

		/// <summary>
		/// replaces [img]attX[/img] tags with [img]getattachment.ashx?FileID=FileID[/img]
		/// </summary>
		/// <param name="messageBody"></param>
		/// <param name="filesAttached"></param>
		/// <returns></returns>
		public static string FormatInlineAttachmetns(string messageBody, DataView filesAttached)
		{
			string output = messageBody;
			for (int i = 0; i < filesAttached.Count; i++)
			{
				string fileId = filesAttached[i]["FileID"].ToString();
				output = output.Replace(string.Format("[img]att{0}[/img]", i + 1), string.Format("[img]getattachment.ashx?FileID={0}[/img]", fileId));
			}
			return output;
		}

		public static string FormatInlineAttachmetns(string messageBody, int messageId)
		{
			if(!messageBody.Contains("[img]att")) return messageBody; //to save DB query when there's no inline images in a message

			using (var cn = DB.CreateOpenConnection())
			{
				var dr = cn.ExecuteReader("SELECT FileID FROM ForumUploadedFiles WHERE MessageID=?", messageId);

				var dt = new DataTable();
				dt.Load(dr);
				dr.Close();
				cn.Close();

				return FormatInlineAttachmetns(messageBody, dt.DefaultView);
			}
		}

		//the method formats... argh! see the code, it is self-explainig
		public static string FormatSmilies(string input)
		{
			string msg = input;
			msg = msg.Replace(":)", "<img src=\"images/smilies/smile.gif\" border=0 alt=\"smile\" />");
			msg = msg.Replace(";)", "<img src=\"images/smilies/wink.gif\" border=0 alt=\"wink\" />");
			msg = msg.Replace(":(", "<img src=\"images/smilies/upset.gif\" border=0 alt=\"upset\" />");
			msg = msg.Replace(":beer:", "<img src=\"images/smilies/beer.gif\" border=\"0\" alt=\"beer\" />");
			msg = msg.Replace(":bong:", "<img src=\"images/smilies/bongL3i8.gif\" border=\"0\" alt=\"Bong\" />");
			msg = msg.Replace(":cheers:", "<img src=\"images/smilies/28208052075.gif\" border=\"0\" alt=\"Toast\" />");
			msg = msg.Replace(":coffee:", "<img src=\"images/smilies/coffee26at.gif\" border=\"0\" alt=\"Drink\" />");
			msg = msg.Replace(":drop:", "<img src=\"images/smilies/wiggle.gif\" border=\"0\" alt=\"Wiggle\" />");
			msg = msg.Replace(":gossip:", "<img src=\"images/smilies/5822044956.gif\" border=\"0\" alt=\"Gossip\" />");
			msg = msg.Replace(":popcorn:", "<img src=\"images/smilies/5821542753.gif\" border=\"0\" alt=\"Eat popcorn\" />");
			msg = msg.Replace(":report:", "<img src=\"images/smilies/5822282438.gif\" border=\"0\" alt=\"Read Report\" />");
			msg = msg.Replace(":secret:", "<img src=\"images/smilies/ssst.gif\" border=\"0\" alt=\"Quiet\" />");
			msg = msg.Replace(":sleep:", "<img src=\"images/smilies/27418101277.gif\" border=\"0\" alt=\"Sleepy\" />");
			msg = msg.Replace(":smoke:", "<img src=\"images/smilies/spliff.gif\" border=\"0\" alt=\"Cool Smoke\" />");
			msg = msg.Replace(":sugar:", "<img src=\"images/smilies/5822332967.gif\" border=\"0\" alt=\"Sugar High\" />");
			msg = msg.Replace(":wave:", "<img src=\"images/smilies/wavey.gif\" border=\"0\" alt=\"Wave\" />");
			msg = msg.Replace(":wings:", "<img src=\"images/smilies/angel2.gif\" border=\"0\" alt=\"Angel Wings\" />");
			msg = msg.Replace(":applause:", "<img src=\"images/smilies/32308364851.gif\" border=\"0\" alt=\"Applause\" />");
			msg = msg.Replace(":bow:", "<img src=\"images/smilies/bowdown.gif\" border=\"0\" alt=\"bow\" />");
			msg = msg.Replace(":buddies:", "<img src=\"images/smilies/dri0047.gif\" border=\"0\" alt=\"Buddies\" />");
			msg = msg.Replace(":buttstr:", "<img src=\"images/smilies/5821472746.gif\" border=\"0\" alt=\"Buttstroke\" />");
			msg = msg.Replace(":good:", "<img src=\"images/smilies/goodpost7td.gif\" border=\"0\" alt=\"Good Posting\" />");
			msg = msg.Replace(":iagree:", "<img src=\"images/smilies/iagree.gif\" border=\"0\" alt=\"i agree\" />");
			msg = msg.Replace(":iws:", "<img src=\"images/smilies/stupid.gif\" border=\"0\" alt=\"i'm with stupid\" />");
			msg = msg.Replace(":logic:", "<img src=\"images/smilies/13523300456.gif\" border=\"0\" alt=\"Logic\" />");
			msg = msg.Replace(":pathead:", "<img src=\"images/smilies/pat9xu.gif\" border=\"0\" alt=\"Pat on the head\" />");
			msg = msg.Replace(":thumb:", "<img src=\"images/smilies/thumb.gif\" border=\"0\" alt=\"Thumbs Up\" />");
			msg = msg.Replace(":whs:", "<img src=\"images/smilies/whs0be.gif\" border=\"0\" alt=\"What He Said\" />");
			msg = msg.Replace(":worship:", "<img src=\"images/smilies/worshippy.gif\" border=\"0\" alt=\"worship\" />");
			msg = msg.Replace(":badrazz:", "<img src=\"images/smilies/badrazz.gif\" border=\"0\" alt=\"bad razz\" />");
			msg = msg.Replace(":blah:", "<img src=\"images/smilies/metallicblue.gif\" border=\"0\" alt=\"blah\" />");
			msg = msg.Replace(":bricks:", "<img src=\"images/smilies/sterb0734ps.gif\" border=\"0\" alt=\"Ton of Bricks\" />");
			msg = msg.Replace(":forkoff:", "<img src=\"images/smilies/fork_off.gif\" border=\"0\" alt=\"Fork Off\" />");
			msg = msg.Replace(":lamer:", "<img src=\"images/smilies/lamer2845nh.gif\" border=\"0\" alt=\"Lamer noob\" />");
			msg = msg.Replace(":nono:", "<img src=\"images/smilies/hsnono.gif\" border=\"0\" alt=\"nono\" />");
			msg = msg.Replace(":owned:", "<img src=\"images/smilies/owned.gif\" border=\"0\" alt=\"Owned\" />");
			msg = msg.Replace(":shakehead:", "<img src=\"images/smilies/shakehead.gif\" border=\"0\" alt=\"shake head\" />");
			msg = msg.Replace(":shutup:", "<img src=\"images/smilies/5822291454.gif\" border=\"0\" alt=\"Shut It\" />");
			msg = msg.Replace(":slap:", "<img src=\"images/smilies/wtcslap.gif\" border=\"0\" alt=\"slap\" />");
			msg = msg.Replace(":smash:", "<img src=\"images/smilies/smash.gif\" border=\"0\" alt=\"Gavel\" />");
			msg = msg.Replace(":thumb-:", "<img src=\"images/smilies/thumbdowncopy1up.gif\" border=\"0\" alt=\"Thumbs Down\" />");
			msg = msg.Replace(":yas:", "<img src=\"images/smilies/urstupid.gif\" border=\"0\" alt=\"You're Stupid\" />");
			msg = msg.Replace(":argue:", "<img src=\"images/smilies/argue.gif\" border=\"0\" alt=\"Argument\" />");
			msg = msg.Replace(":boxer:", "<img src=\"images/smilies/boxer.gif\" border=\"0\" alt=\"boxer\" />");
			msg = msg.Replace(":fencing:", "<img src=\"images/smilies/6804382843.gif\" border=\"0\" alt=\"En Garde!\" />");
			msg = msg.Replace(":flame:", "<img src=\"images/smilies/sterb2457li.gif\" border=\"0\" alt=\"Fart/Flame the noob\" />");
			msg = msg.Replace(":goaway:", "<img src=\"images/smilies/30416221069.gif\" border=\"0\" alt=\"Go Away\" />");
			msg = msg.Replace(":guns:", "<img src=\"images/smilies/sterb1842sg.gif\" border=\"0\" alt=\"Gun\" />");
			msg = msg.Replace(":gunsling:", "<img src=\"images/smilies/sterb1908nd.gif\" border=\"0\" alt=\"Gunslinger\" />");
			msg = msg.Replace(":headbite:", "<img src=\"images/smilies/5122424636.gif\" border=\"0\" alt=\"Bite your head off\" />");
			msg = msg.Replace(":lightsab:", "<img src=\"images/smilies/sterb0298yz.gif\" border=\"0\" alt=\"Darth Lightsabers\" />");
			msg = msg.Replace(":mob:", "<img src=\"images/smilies/5422184119.gif\" border=\"0\" alt=\"Angry Mob\" />");
			msg = msg.Replace(":needmod:", "<img src=\"images/smilies/582145129.gif\" border=\"0\" alt=\"Belittle\" />");
			msg = msg.Replace(":nutkick:", "<img src=\"images/smilies/nutkick5ur.gif\" border=\"0\" alt=\"Nut Kick\" />");
			msg = msg.Replace(":pillow:", "<img src=\"images/smilies/30700402927.gif\" border=\"0\" alt=\"Pillow Fight\" />");
			msg = msg.Replace(":smack:", "<img src=\"images/smilies/5700365222.gif\" border=\"0\" alt=\"Smack!\" />");
			msg = msg.Replace(":stooge:", "<img src=\"images/smilies/sterb1918mv.gif\" border=\"0\" alt=\"3 Stooges\" />");
			msg = msg.Replace(":whack:", "<img src=\"images/smilies/sterb1881bq.gif\" border=\"0\" alt=\"Hammer Time\" />");
			msg = msg.Replace(":wuss:", "<img src=\"images/smilies/5822392499.gif\" border=\"0\" alt=\"Wuss Fight\" />");
			msg = msg.Replace(":blush:", "<img src=\"images/smilies/5821461258.gif\" border=\"0\" alt=\"Blush\" />");
			msg = msg.Replace(":boink:", "<img src=\"images/smilies/boink.gif\" border=\"0\" alt=\"boink\" />");
			msg = msg.Replace(":crazy:", "<img src=\"images/smilies/confused7nt.gif\" border=\"0\" alt=\"Bit Wonky..\" />");
			msg = msg.Replace(":eek:", "<img src=\"images/smilies/eek.gif\" border=\"0\" alt=\"EEK!\" />");
			msg = msg.Replace(":foilhat:", "<img src=\"images/smilies/tinfoil.gif\" border=\"0\" alt=\"Tin Foil Hat\" />");
			msg = msg.Replace(":gotcha:", "<img src=\"images/smilies/gotcha.gif\" border=\"0\" alt=\"Gotcha!\" />");
			msg = msg.Replace(":greenp:", "<img src=\"images/smilies/crazy3.gif\" border=\"0\" alt=\"Green Tongue\" />");
			msg = msg.Replace(":grngreedy:", "<img src=\"images/smilies/grngreedy.gif\" border=\"0\" alt=\"Greedy Guts\" />");
			msg = msg.Replace(":hahano:", "<img src=\"images/smilies/hahano.gif\" border=\"0\" alt=\"hahano\" />");
			msg = msg.Replace(":ignore:", "<img src=\"images/smilies/ignore.gif\" border=\"0\" alt=\"Ignored\" />");
			msg = msg.Replace(":loo:", "<img src=\"images/smilies/loo.gif\" border=\"0\" alt=\"Loo Flush\" />");
			msg = msg.Replace(":looks:", "<img src=\"images/smilies/spyme.gif\" border=\"0\" alt=\"Creeped out\" />");
			msg = msg.Replace(":menace:", "<img src=\"images/smilies/menacegrin.gif\" border=\"0\" alt=\"Menacing\" />");
			msg = msg.Replace(":omg:", "<img src=\"images/smilies/omg9hi.gif\" border=\"0\" alt=\"OMG\" />");
			msg = msg.Replace(":peace:", "<img src=\"images/smilies/Peace!.gif\" border=\"0\" alt=\"Peace Sign\" />");
			msg = msg.Replace(":poke:", "<img src=\"images/smilies/stickpoke.gif\" border=\"0\" alt=\"poke\" />");
			msg = msg.Replace(":puppy:", "<img src=\"images/smilies/sdb60030.gif\" border=\"0\" alt=\"puppy dog eyes\" />");
			msg = msg.Replace(":rolleyes:", "<img src=\"images/smilies/rolleyes.gif\" border=\"0\" alt=\"Roll Eyes (Sarcastic)\" />");
			msg = msg.Replace(":shocked:", "<img src=\"images/smilies/SHOCKED.gif\" border=\"0\" alt=\"Shock\" />");
			msg = msg.Replace(":sick:", "<img src=\"images/smilies/ill.gif\" border=\"0\" alt=\"Sick\" />");
			msg = msg.Replace(":silly:", "<img src=\"images/smilies/silly.gif\" border=\"0\" alt=\"Goofus\" />");
			msg = msg.Replace(":twitch:", "<img src=\"images/smilies/twitch2.gif\" border=\"0\" alt=\"Twitchy\" />");
			msg = msg.Replace(":/:", "<img src=\"images/smilies/_sure.gif\" border=\"0\" alt=\"Riiiight.\" />");
			msg = msg.Replace(":\\:", "<img src=\"images/smilies/eek7.gif\" border=\"0\" alt=\"Whaaaaa?\" />");
			msg = msg.Replace(":confused:", "<img src=\"images/smilies/confused.gif\" border=\"0\" alt=\"Confused\" />");
			msg = msg.Replace(":doh:", "<img src=\"images/smilies/4915593391.gif\" border=\"0\" alt=\"slap forehead\" />");
			msg = msg.Replace(":duh:", "<img src=\"images/smilies/5700272664.gif\" border=\"0\" alt=\"Duhh\" />");
			msg = msg.Replace(":dunno:", "<img src=\"images/smilies/dunno.gif\" border=\"0\" alt=\"dunno\" />");
			msg = msg.Replace(":headscrat:", "<img src=\"images/smilies/headscratch.gif\" border=\"0\" alt=\"hmm\" />");
			msg = msg.Replace(":nosmile:", "<img src=\"images/smilies/nosmile.gif\" border=\"0\" alt=\"Blank stare\" />");
			msg = msg.Replace(":shrug:", "<img src=\"images/smilies/icon_darin.gif\" border=\"0\" alt=\"shrug\" />");
			msg = msg.Replace(":squint:", "<img src=\"images/smilies/squint.gif\" border=\"0\" alt=\"squint\" />");
			msg = msg.Replace(":werd:", "<img src=\"images/smilies/werd.gif\" border=\"0\" alt=\"werd\" />");
			msg = msg.Replace(":D", "<img src=\"images/smilies/biggrin.gif\" border=\"0\" alt=\"Big Grin\" />");
			msg = msg.Replace(":fingersx:", "<img src=\"images/smilies/fingersx.gif\" border=\"0\" alt=\"fingers crossed\" />");
			msg = msg.Replace(":gh:", "<img src=\"images/smilies/grouphug.gif\" border=\"0\" alt=\"grouphug\" />");
			msg = msg.Replace(":glomp:", "<img src=\"images/smilies/5822051440.gif\" border=\"0\" alt=\"Glomp, Hi!\" />");
			msg = msg.Replace(":hitit:", "<img src=\"images/smilies/hitit.gif\" border=\"0\" alt=\"hitit\" />");
			msg = msg.Replace(":hug:", "<img src=\"images/smilies/hug.gif\" border=\"0\" alt=\"hug\" />");
			msg = msg.Replace(":inlove:", "<img src=\"images/smilies/5822214714.gif\" border=\"0\" alt=\"In Love\" />");
			msg = msg.Replace(":kissyou:", "<img src=\"images/smilies/kissyou.gif\" border=\"0\" alt=\"Kissing\" />");
			msg = msg.Replace(":naughty:", "<img src=\"images/smilies/naughty.gif\" border=\"0\" alt=\"naughty\" />");
			msg = msg.Replace(":-P", "<img src=\"images/smilies/tongue.gif\" border=\"0\" alt=\"Stick Out Tongue\" />");
			msg = msg.Replace(":woohoo:", "<img src=\"images/smilies/woohoo.gif\" border=\"0\" alt=\"Woo Hoo!\" />");
			msg = msg.Replace(":argh:", "<img src=\"images/smilies/sd3.gif\" border=\"0\" alt=\"RRRGH\" />");
			msg = msg.Replace(":banghead:", "<img src=\"images/smilies/banghead.gif\" border=\"0\" alt=\"Brick Wall\" />");
			msg = msg.Replace(":cry:", "<img src=\"images/smilies/908572171.gif\" border=\"0\" alt=\"Cry\" />");
			msg = msg.Replace(":curse:", "<img src=\"images/smilies/28510172849.gif\" border=\"0\" alt=\"Cursing\" />");
			msg = msg.Replace(":mad:", "<img src=\"images/smilies/po.gif\" border=\"0\" alt=\"Mad\" />");
			msg = msg.Replace(":rant:", "<img src=\"images/smilies/rant2.gif\" border=\"0\" alt=\"rant\" />");
			msg = msg.Replace(":sweat:", "<img src=\"images/smilies/newbluesweatdrop.gif\" border=\"0\" alt=\"Anime Sweat\" />");
			msg = msg.Replace(":hahabow:", "<img src=\"images/smilies/bowrofl.gif\" border=\"0\" alt=\"bowrofl\" />");
			msg = msg.Replace(":hahaha:", "<img src=\"images/smilies/hahaha.gif\" border=\"0\" alt=\"hahaha\" />");
			msg = msg.Replace(":heh_heh:", "<img src=\"images/smilies/heh_heh.gif\" border=\"0\" alt=\"heh heh\" />");
			msg = msg.Replace(":hehe:", "<img src=\"images/smilies/kekekegay.gif\" border=\"0\" alt=\"hehe\" />");
			msg = msg.Replace(":jk:", "<img src=\"images/smilies/5903011026.gif\" border=\"0\" alt=\"Just Kidding\" />");
			msg = msg.Replace(":lolhit:", "<img src=\"images/smilies/5700293539.gif\" border=\"0\" alt=\"lol hit\" />");
			msg = msg.Replace(":rofl:", "<img src=\"images/smilies/laugh.gif\" border=\"0\" alt=\"laugh\" />");
			msg = msg.Replace(":roflmao:", "<img src=\"images/smilies/13501381245.gif\" border=\"0\" alt=\"ROFLMAO\" />");
			msg = msg.Replace(":404:", "<img src=\"images/smilies/404.gif\" border=\"0\" alt=\"Not Found\" />");
			msg = msg.Replace(":abuse:", "<img src=\"images/smilies/28517070443.gif\" border=\"0\" alt=\"S&M Abuse\" />");
			msg = msg.Replace(":damnpc:", "<img src=\"images/smilies/damnpc.gif\" border=\"0\" alt=\"Damn Computer..\" />");
			msg = msg.Replace(":milk:", "<img src=\"images/smilies/milk.gif\" border=\"0\" alt=\"Drink Milk\" />");
			msg = msg.Replace(":skull:", "<img src=\"images/smilies/bgmad.gif\" border=\"0\" alt=\"Skull\" />");
			msg = msg.Replace(":unclesam:", "<img src=\"images/smilies/UNCLESAM.gif\" border=\"0\" alt=\"Uncle Sam\" />");
			msg = msg.Replace(":usa:", "<img src=\"images/smilies/usa.gif\" border=\"0\" alt=\"Yankee\" />");
			msg = msg.Replace(":violin:", "<img src=\"images/smilies/7314474053.gif\" border=\"0\" alt=\"Violin\" />");
			msg = msg.Replace(":dance:", "<img src=\"images/smilies/dance.gif\" border=\"0\" alt=\"Boogy Dance\" />");
			msg = msg.Replace(":dawave:", "<img src=\"images/smilies/dawave.gif\" border=\"0\" alt=\"The Wave\" />");
			msg = msg.Replace(":hb:", "<img src=\"images/smilies/birthday.gif\" border=\"0\" alt=\"happy birthday\" />");
			msg = msg.Replace(":jammin:", "<img src=\"images/smilies/jammin.gif\" border=\"0\" alt=\"Jammin'\" />");
			msg = msg.Replace(":music:", "<img src=\"images/smilies/music-smiley-026.gif\" border=\"0\" alt=\"Rock Band\" />");
			msg = msg.Replace(":party:", "<img src=\"images/smilies/party.gif\" border=\"0\" alt=\"Party\" />");
			msg = msg.Replace(":rockon:", "<img src=\"images/smilies/ylsuper.gif\" border=\"0\" alt=\"rock on\" />");
			msg = msg.Replace(":cool:", "<img src=\"images/smilies/1cool.gif\" border=\"0\" alt=\"Cool\" />");
			msg = msg.Replace(":educated:", "<img src=\"images/smilies/educate.gif\" border=\"0\" alt=\"Educated\" />");
			msg = msg.Replace(":flasher:", "<img src=\"images/smilies/flasher.gif\" border=\"0\" alt=\"Flasher\" />");
			msg = msg.Replace(":froot:", "<img src=\"images/smilies/fruit.gif\" border=\"0\" alt=\"Froot\" />");
			msg = msg.Replace(":king:", "<img src=\"images/smilies/king.gif\" border=\"0\" alt=\"King-dude\" />");
			msg = msg.Replace(":king2:", "<img src=\"images/smilies/knee7rm.gif\" border=\"0\" alt=\"Kneel!\" />");
			msg = msg.Replace(":master:", "<img src=\"images/smilies/overlord.gif\" border=\"0\" alt=\"King-tron\" />");
			msg = msg.Replace(":pimp:", "<img src=\"images/smilies/pimp.gif\" border=\"0\" alt=\"P.I.M.P.\" />");
			msg = msg.Replace(":pirate:", "<img src=\"images/smilies/pir8.gif\" border=\"0\" alt=\"Avast!\" />");
			msg = msg.Replace(":police:", "<img src=\"images/smilies/police.gif\" border=\"0\" alt=\"Police\" />");
			msg = msg.Replace(":spock:", "<img src=\"images/smilies/spock.gif\" border=\"0\" alt=\"Spock\" />");
			msg = msg.Replace(":storm:", "<img src=\"images/smilies/5420285018.gif\" border=\"0\" alt=\"Stormtrooper\" />");
			msg = msg.Replace(":faq:", "<img src=\"images/smilies/faqnice.gif\" border=\"0\" alt=\"FAQ Nice\" />");
			msg = msg.Replace(":google:", "<img src=\"images/smilies/google.gif\" border=\"0\" alt=\"google\" />");
			msg = msg.Replace(":rtfm:", "<img src=\"images/smilies/rtfm.gif\" border=\"0\" alt=\"RTFM\" />");
			msg = msg.Replace(":rulez:", "<img src=\"images/smilies/rulez.gif\" border=\"0\" alt=\"Rulez Nice\" />");
			msg = msg.Replace(":wiki:", "<img src=\"images/smilies/wiki.gif\" border=\"0\" alt=\"Wikipedia\" />");
			msg = msg.Replace(":banhim:", "<img src=\"images/smilies/banhim.gif\" border=\"0\" alt=\"banhim\" />");
			msg = msg.Replace(":banned:", "<img src=\"images/smilies/banned.gif\" border=\"0\" alt=\"Ban Stamp\" />");
			msg = msg.Replace(":bump:", "<img src=\"images/smilies/bump.gif\" border=\"0\" alt=\"Bump\" />");
			msg = msg.Replace(":double:", "<img src=\"images/smilies/dbl.gif\" border=\"0\" alt=\"Double Thread/Post\" />");
			msg = msg.Replace(":edit:", "<img src=\"images/smilies/edited.gif\" border=\"0\" alt=\"Edit\" />");
			msg = msg.Replace(":feedback:", "<img src=\"images/smilies/feedback.gif\" border=\"0\" alt=\"Feedback Requested\" />");
			msg = msg.Replace(":flogging:", "<img src=\"images/smilies/flogging.gif\" border=\"0\" alt=\"Flog dead topic\" />");
			msg = msg.Replace(":hijack:", "<img src=\"images/smilies/threadjacked.gif\" border=\"0\" alt=\"Thread Hijack!\" />");
			msg = msg.Replace(":locked:", "<img src=\"images/smilies/lockd.gif\" border=\"0\" alt=\"lockd\" />");
			msg = msg.Replace(":mods:", "<img src=\"images/smilies/7309300734.gif\" border=\"0\" alt=\"Beware of the Mods...they come in the Niiiiiiight!\" />");
			msg = msg.Replace(":offtopic:", "<img src=\"images/smilies/offtopic.gif\" border=\"0\" alt=\"Off Topic\" />");
			msg = msg.Replace(":ontopic:", "<img src=\"images/smilies/ontopic.gif\" border=\"0\" alt=\"On Topic\" />");
			msg = msg.Replace(":qfe:", "<img src=\"images/smilies/qfe.gif\" border=\"0\" alt=\"Quoted For Emphasis\" />");
			msg = msg.Replace(":repost:", "<img src=\"images/smilies/repost.gif\" border=\"0\" alt=\"repost\" />");
			msg = msg.Replace(":spam:", "<img src=\"images/smilies/spam4ot.gif\" border=\"0\" alt=\"Spam Alert!\" />");
			msg = msg.Replace(":spamkill:", "<img src=\"images/smilies/4913420063.gif\" border=\"0\" alt=\"Die die die!\" />");
			return msg;
		}

		public static bool ContainsBadWords(string input)
		{
			return ContainsBadWords(input, Settings.BadWords);
		}

		public static bool ContainsBadWords(string input, string[] badWords)
		{
			if (badWords == null) return false;

			foreach (string bw in badWords)
			{
				if (!string.IsNullOrEmpty(bw))
				{
					if (Regex.IsMatch(input, @"(^|\W)" + Regex.Escape(bw) + @"($|\W)", RegexOptions.IgnoreCase)) return true;
				}
			}
			return false;
		}
	}
}
