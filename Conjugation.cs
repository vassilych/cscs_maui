using System;
using ScriptingMaui.Resources.Strings;

namespace ScriptingMaui;

public partial class LearnPage : ContentPage
{
    async void InfoData_Clicked(object? sender, EventArgs e)
    {
        StopPlay();
        var words = Context.Word.GetTranslation(SettingsPage.VoiceLearn);
        var tok = words.Split(",", StringSplitOptions.TrimEntries);
        m_conjVerbs = tok.ToList();
        await Conjugate();
    }

    void ShowRow3(bool isVisible = true)
    {
        VerbInfoH7.IsVisible = VerbInfoH8.IsVisible = VerbInfoH9.IsVisible =
            Border31.IsVisible = Border32.IsVisible = Border33.IsVisible = isVisible;
        VerbInfo131.IsVisible = VerbInfo141.IsVisible = VerbInfo151.IsVisible = VerbInfo161.IsVisible = VerbInfo171.IsVisible = VerbInfo181.IsVisible = isVisible;
        VerbInfo132.IsVisible = VerbInfo142.IsVisible = VerbInfo152.IsVisible = VerbInfo162.IsVisible = VerbInfo172.IsVisible = VerbInfo182.IsVisible = isVisible;
        VerbInfo133.IsVisible = VerbInfo143.IsVisible = VerbInfo153.IsVisible = VerbInfo163.IsVisible = VerbInfo173.IsVisible = VerbInfo183.IsVisible = isVisible;
        Border131.IsVisible = Border141.IsVisible = Border151.IsVisible = Border161.IsVisible = Border171.IsVisible = Border181.IsVisible = isVisible;
        Border132.IsVisible = Border142.IsVisible = Border152.IsVisible = Border162.IsVisible = Border172.IsVisible = Border182.IsVisible = isVisible;
        Border133.IsVisible = Border143.IsVisible = Border153.IsVisible = Border163.IsVisible = Border173.IsVisible = Border183.IsVisible = isVisible;
        if (!isVisible)
        {
            VerbInfoH7.Text = VerbInfoH8.Text = VerbInfoH9.Text = "";
            VerbInfo131.Text = VerbInfo141.Text = VerbInfo151.Text = VerbInfo161.Text = VerbInfo171.Text = VerbInfo181.Text = "";
            VerbInfo132.Text = VerbInfo142.Text = VerbInfo152.Text = VerbInfo162.Text = VerbInfo172.Text = VerbInfo182.Text = "";
            VerbInfo133.Text = VerbInfo143.Text = VerbInfo153.Text = VerbInfo163.Text = VerbInfo173.Text = VerbInfo183.Text = "";
        }
    }

    async Task<bool> Conjugate()
    {
        if (m_conjVerbs.Count == 0)
        {
            return true;
        }
        var prefix1 = SettingsPage.VoiceLearn.Substring(0, 2);
        var prefix2 = SettingsPage.MyVoice.Substring(0, 2);

        var verb = m_conjVerbs.First();
        m_conjVerbs.RemoveAt(0);

        var data = GetWordData(prefix1, verb);
        if (string.IsNullOrWhiteSpace(data))
        {
            if (SettingsPage.VoiceLearn == "de-CH")
            {
                var words = Context.Word.GetTranslation("de-DE");
                var t = words.Split(",", StringSplitOptions.TrimEntries);
                data = GetWordData(prefix1, t.First());
            }
            if (string.IsNullOrWhiteSpace(data))
            {
                var c = verb.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var cand2 = c.First();
                data = GetWordData(prefix1, cand2);
            }
            if (string.IsNullOrWhiteSpace(data))
            {
                await DisplayAlert(AppResources.verbs, string.Format(AppResources.Word_with__0__not_found, verb), "OK");
                m_conjVerbs.Remove(verb);
                return m_conjVerbs.Count == 0;
            }
        }
        SetMode(false, true);
        m_conjVerbs.Remove(verb);
        var tokens = data.Trim().Split(new char[] { ',' }, (StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        if (prefix1.StartsWith("en"))
        {
            LoadEnVerbs(tokens);
        }
        else if (prefix1.StartsWith("es"))
        {
            LoadEsVerbs(tokens);
        }
        else if (prefix1.StartsWith("it"))
        {
            LoadItVerbs(tokens);
        }
        else if (prefix1.StartsWith("fr"))
        {
            LoadFrVerbs(tokens);
        }
        else if (prefix1.StartsWith("pt"))
        {
            LoadPtVerbs(tokens);
        }
        else if (prefix1.StartsWith("de"))
        {
            LoadDeVerbs(tokens);
        }
        else if (prefix1.StartsWith("ru"))
        {
            LoadRuVerbs(tokens);
        }

        SetSize(VerbInfoH1, true); SetSize(VerbInfoH2, true); SetSize(VerbInfoH3, true);
        SetSize(VerbInfoH4, true); SetSize(VerbInfoH5, true); SetSize(VerbInfoH6, true);
        SetSize(VerbInfoH7, true); SetSize(VerbInfoH8, true); SetSize(VerbInfoH9, true);

        SetSize(VerbInfo11); SetSize(VerbInfo21); SetSize(VerbInfo31);
        SetSize(VerbInfo12); SetSize(VerbInfo22); SetSize(VerbInfo32);
        SetSize(VerbInfo13); SetSize(VerbInfo23); SetSize(VerbInfo33);
        SetSize(VerbInfo41); SetSize(VerbInfo51); SetSize(VerbInfo61);
        SetSize(VerbInfo42); SetSize(VerbInfo52); SetSize(VerbInfo62);
        SetSize(VerbInfo43); SetSize(VerbInfo53); SetSize(VerbInfo63);

        SetSize(VerbInfo71); SetSize(VerbInfo81); SetSize(VerbInfo91);
        SetSize(VerbInfo72); SetSize(VerbInfo82); SetSize(VerbInfo92);
        SetSize(VerbInfo73); SetSize(VerbInfo83); SetSize(VerbInfo93);
        SetSize(VerbInfo101); SetSize(VerbInfo111); SetSize(VerbInfo121);
        SetSize(VerbInfo102); SetSize(VerbInfo112); SetSize(VerbInfo122);
        SetSize(VerbInfo103); SetSize(VerbInfo113); SetSize(VerbInfo123);

        SetSize(VerbInfo131); SetSize(VerbInfo141); SetSize(VerbInfo151);
        SetSize(VerbInfo132); SetSize(VerbInfo142); SetSize(VerbInfo152);
        SetSize(VerbInfo133); SetSize(VerbInfo143); SetSize(VerbInfo153);
        SetSize(VerbInfo161); SetSize(VerbInfo171); SetSize(VerbInfo181);
        SetSize(VerbInfo162); SetSize(VerbInfo172); SetSize(VerbInfo182);
        SetSize(VerbInfo163); SetSize(VerbInfo173); SetSize(VerbInfo183);

        return false;
    }
    void LoadEnVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Particip: " + tokens[2];
        Gerund.Text = "Gerund: " + tokens[3];
        // to be, be, been, being, am, are, is, was, were, was
        VerbInfoH1.Text = "Present";
        VerbInfo11.Text = "I " + tokens[4];
        VerbInfo21.Text = "you " + tokens[5];
        VerbInfo31.Text = "she " + tokens[6];
        VerbInfo41.Text = "we " + tokens[5];
        VerbInfo51.Text = "you " + tokens[5];
        VerbInfo61.Text = "they " + tokens[5];

        VerbInfoH2.Text = "Simple Past";
        VerbInfo12.Text = "I " + tokens[7];
        VerbInfo22.Text = "you " + tokens[8];
        VerbInfo32.Text = "she " + tokens[9];
        VerbInfo42.Text = "we " + tokens[7];
        VerbInfo52.Text = "you " + tokens[7];
        VerbInfo62.Text = "they " + tokens[7];

        VerbInfoH3.Text = "Future";
        VerbInfo13.Text = "I will " + tokens[1];
        VerbInfo23.Text = "you will " + tokens[1];
        VerbInfo33.Text = "she will " + tokens[1];
        VerbInfo43.Text = "we will " + tokens[1];
        VerbInfo53.Text = "you will " + tokens[1];
        VerbInfo63.Text = "they will " + tokens[1];

        VerbInfoH4.Text = "Pres. Cont.";
        VerbInfo71.Text = "I'm " + tokens[3];
        VerbInfo81.Text = "you're " + tokens[3];
        VerbInfo91.Text = "she's " + tokens[3];
        VerbInfo101.Text = "we're " + tokens[3];
        VerbInfo111.Text = "you're " + tokens[3];
        VerbInfo121.Text = "they're " + tokens[3];

        VerbInfoH5.Text = "Pres. Perfect";
        VerbInfo72.Text = "have " + tokens[2];
        VerbInfo82.Text = "have " + tokens[2];
        VerbInfo92.Text = "has " + tokens[2];
        VerbInfo102.Text = VerbInfo112.Text = VerbInfo122.Text = "have " + tokens[2];

        VerbInfoH6.Text = "Conditional";
        VerbInfo73.Text = VerbInfo83.Text = VerbInfo93.Text =
            VerbInfo103.Text = VerbInfo113.Text = VerbInfo123.Text = "would " + tokens[1];

        ShowRow3(true);
        VerbInfoH7.Text = "Past Cont.";
        VerbInfo131.Text = "I was " + tokens[3];
        VerbInfo141.Text = "you were " + tokens[3];
        VerbInfo151.Text = "she was " + tokens[3];
        VerbInfo161.Text = "we were " + tokens[3];
        VerbInfo171.Text = "you were " + tokens[3];
        VerbInfo181.Text = "they were " + tokens[3];

        VerbInfoH8.Text = "Past Perfect";
        VerbInfo132.Text = "had " + tokens[2];
        VerbInfo142.Text = "had " + tokens[2];
        VerbInfo152.Text = "had " + tokens[2];
        VerbInfo162.Text = VerbInfo172.Text = VerbInfo182.Text = "had " + tokens[2];

        VerbInfoH9.Text = "Pres. Emphatic";
        VerbInfo133.Text = "do " + tokens[1];
        VerbInfo143.Text = "do " + tokens[1];
        VerbInfo153.Text = "does " + tokens[1];
        VerbInfo163.Text = "do " + tokens[1];
        VerbInfo173.Text = "do " + tokens[1];
        VerbInfo183.Text = "do " + tokens[1];
    }

    void LoadEsVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Participio: " + tokens[1];
        Gerund.Text = "Gerundio: " + tokens[2];
        /* hacer, hecho, haciendo, hago,haces,hace,hacemos,hacéis,hacen,  hacía,hacías,hacía,hacíamos,hacíais,hacían,
         15 hice,hiciste,hizo,hicimos,hicisteis,hicieron, haré,harás,hará,haremos,haréis,harán,
         27 hecho,haría,harías,haría,haríamos,haríais,harían */
        VerbInfoH1.Text = "Presente";
        VerbInfo11.Text = "Yo " + tokens[3];
        VerbInfo21.Text = "Tú " + tokens[4];
        VerbInfo31.Text = "Él/Ella/Ud " + tokens[5];
        VerbInfo41.Text = "Nosotros " + tokens[6];
        VerbInfo51.Text = "Vosotros " + tokens[7];
        VerbInfo61.Text = "Ellas/Ustedes " + tokens[8];

        VerbInfoH2.Text = "Pret Imperfecto";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];

        VerbInfoH3.Text = "Pret Indefinido";
        VerbInfo13.Text = tokens[15];
        VerbInfo23.Text = tokens[16];
        VerbInfo33.Text = tokens[17];
        VerbInfo43.Text = tokens[18];
        VerbInfo53.Text = tokens[19];
        VerbInfo63.Text = tokens[20];

        VerbInfoH4.Text = "Futuro";
        VerbInfo71.Text = tokens[21];
        VerbInfo81.Text = tokens[22];
        VerbInfo91.Text = tokens[23];
        VerbInfo101.Text = tokens[24];
        VerbInfo111.Text = tokens[25];
        VerbInfo121.Text = tokens[26];

        VerbInfoH5.Text = "Pret Perfecto";
        VerbInfo72.Text = "he " + tokens[1];
        VerbInfo82.Text = "has " + tokens[1];
        VerbInfo92.Text = "ha " + tokens[1];
        VerbInfo102.Text = "hemos " + tokens[1];
        VerbInfo112.Text = "habéis " + tokens[1];
        VerbInfo122.Text = "han " + tokens[1];

        VerbInfoH6.Text = "Condicional";
        VerbInfo73.Text = tokens[27];
        VerbInfo83.Text = tokens[28];
        VerbInfo93.Text = tokens[29];
        VerbInfo103.Text = tokens[30];
        VerbInfo113.Text = tokens[31];
        VerbInfo123.Text = tokens[32];

        ShowRow3(true);
        VerbInfoH7.Text = "Pluscuamperfecto";
        VerbInfo131.Text = "había " + tokens[1];
        VerbInfo141.Text = "habías " + tokens[1];
        VerbInfo151.Text = "había " + tokens[1];
        VerbInfo161.Text = "habíamos " + tokens[1];
        VerbInfo171.Text = "habíais " + tokens[1];
        VerbInfo181.Text = "habían " + tokens[1];

        VerbInfoH8.Text = "Pres continuo";
        VerbInfo132.Text = "estoy " + tokens[2];
        VerbInfo142.Text = "estás " + tokens[2];
        VerbInfo152.Text = "está " + tokens[2];
        VerbInfo162.Text = "estamos " + tokens[2];
        VerbInfo172.Text = "estáis " + tokens[2];
        VerbInfo182.Text = "están " + tokens[2];

        VerbInfoH9.Text = "Futuro próximo";
        VerbInfo133.Text = "voy a " + tokens[0];
        VerbInfo143.Text = "vas a " + tokens[0];
        VerbInfo153.Text = "va a " + tokens[0];
        VerbInfo163.Text = "vamos a " + tokens[0];
        VerbInfo173.Text = "vais a " + tokens[0];
        VerbInfo183.Text = "van a " + tokens[0];
    }
    void LoadItVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Participio: " + tokens[1];
        Gerund.Text = "Gerundio: " + tokens[2];

        VerbInfoH1.Text = "Presente";
        VerbInfo11.Text = "io " + tokens[3];
        VerbInfo21.Text = "tu " + tokens[4];
        VerbInfo31.Text = "lui/lei " + tokens[5];
        VerbInfo41.Text = "noi " + tokens[6];
        VerbInfo51.Text = "voi " + tokens[7];
        VerbInfo61.Text = "loro " + tokens[8];

        VerbInfoH2.Text = "Passato prossimo";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];

        VerbInfoH3.Text = "Imperfetto";
        VerbInfo13.Text = tokens[15];
        VerbInfo23.Text = tokens[16];
        VerbInfo33.Text = tokens[17];
        VerbInfo43.Text = tokens[18];
        VerbInfo53.Text = tokens[19];
        VerbInfo63.Text = tokens[20];

        VerbInfoH4.Text = "Futuro";
        VerbInfo71.Text = tokens[21];
        VerbInfo81.Text = tokens[22];
        VerbInfo91.Text = tokens[23];
        VerbInfo101.Text = tokens[24];
        VerbInfo111.Text = tokens[25];
        VerbInfo121.Text = tokens[26];

        VerbInfoH5.Text = "Passato remoto";
        VerbInfo72.Text = tokens[27];
        VerbInfo82.Text = tokens[28];
        VerbInfo92.Text = tokens[29];
        VerbInfo102.Text = tokens[30];
        VerbInfo112.Text = tokens[31];
        VerbInfo122.Text = tokens[32];

        VerbInfoH6.Text = "Condizionale pres";
        VerbInfo73.Text = tokens[33];
        VerbInfo83.Text = tokens[34];
        VerbInfo93.Text = tokens[35];
        VerbInfo103.Text = tokens[36];
        VerbInfo113.Text = tokens[37];
        VerbInfo123.Text = tokens[38];

        ShowRow3(true);
        VerbInfoH7.Text = "Congiuntivo pres";
        VerbInfo131.Text = tokens[39];
        VerbInfo141.Text = tokens[40];
        VerbInfo151.Text = tokens[41];
        VerbInfo161.Text = tokens[42];
        VerbInfo171.Text = tokens[43];
        VerbInfo181.Text = tokens[44];

        VerbInfoH8.Text = "Congiuntivo imperf";
        VerbInfo132.Text = tokens[45];
        VerbInfo142.Text = tokens[46];
        VerbInfo152.Text = tokens[47];
        VerbInfo162.Text = tokens[48];
        VerbInfo172.Text = tokens[49];
        VerbInfo182.Text = tokens[50];

        VerbInfoH9.Text = "Imperativo";
        VerbInfo133.Text = "";
        VerbInfo143.Text = "(tu) " + tokens[51];
        VerbInfo153.Text = "(lei)" + tokens[52];
        VerbInfo163.Text = "(noi) " + tokens[53];
        VerbInfo173.Text = "(voi) " + tokens[54];
        VerbInfo183.Text = "(loro) " + tokens[55];
    }
    void LoadFrVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Participe Prés: " + tokens[1];
        Gerund.Text = "Participe Passé: " + tokens[2];

        VerbInfoH1.Text = "Présent";
        VerbInfo11.Text = tokens[3];
        VerbInfo21.Text = tokens[4];
        VerbInfo31.Text = tokens[5];
        VerbInfo41.Text = tokens[6];
        VerbInfo51.Text = tokens[7];
        VerbInfo61.Text = tokens[8];

        VerbInfoH2.Text = "Passé composé";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];

        VerbInfoH3.Text = "Imparfait";
        VerbInfo13.Text = tokens[15];
        VerbInfo23.Text = tokens[16];
        VerbInfo33.Text = tokens[17];
        VerbInfo43.Text = tokens[18];
        VerbInfo53.Text = tokens[19];
        VerbInfo63.Text = tokens[20];

        VerbInfoH4.Text = "Passé simple";
        VerbInfo71.Text = tokens[21];
        VerbInfo81.Text = tokens[22];
        VerbInfo91.Text = tokens[23];
        VerbInfo101.Text = tokens[24];
        VerbInfo111.Text = tokens[25];
        VerbInfo121.Text = tokens[26];

        VerbInfoH5.Text = "Futur";
        VerbInfo72.Text = tokens[27];
        VerbInfo82.Text = tokens[28];
        VerbInfo92.Text = tokens[29];
        VerbInfo102.Text = tokens[30];
        VerbInfo112.Text = tokens[31];
        VerbInfo122.Text = tokens[32];

        VerbInfoH6.Text = "Conditionnel Prés";
        VerbInfo73.Text = tokens[33];
        VerbInfo83.Text = tokens[34];
        VerbInfo93.Text = tokens[35];
        VerbInfo103.Text = tokens[36];
        VerbInfo113.Text = tokens[37];
        VerbInfo123.Text = tokens[38];

        ShowRow3(true);
        VerbInfoH7.Text = "Subjonctif Prés";
        VerbInfo131.Text = tokens[39];
        VerbInfo141.Text = tokens[40];
        VerbInfo151.Text = tokens[41];
        VerbInfo161.Text = tokens[42];
        VerbInfo171.Text = tokens[43];
        VerbInfo181.Text = tokens[44];

        VerbInfoH8.Text = "Subjonctif Imp";
        VerbInfo132.Text = tokens[45];
        VerbInfo142.Text = tokens[46];
        VerbInfo152.Text = tokens[47];
        VerbInfo162.Text = tokens[48];
        VerbInfo172.Text = tokens[49];
        VerbInfo182.Text = tokens[50];

        VerbInfoH9.Text = "Impératif";
        VerbInfo133.Text = "";
        VerbInfo143.Text = tokens[51];
        VerbInfo153.Text = "";
        VerbInfo163.Text = tokens[52];
        VerbInfo173.Text = tokens[53];
        VerbInfo183.Text = "";
    }
    void LoadPtVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Particípio: " + tokens[2];
        Gerund.Text = "Gerúndio: " + tokens[1];

        VerbInfoH1.Text = "Presente";
        VerbInfo11.Text = "eu " + tokens[3];
        VerbInfo21.Text = "tu " + tokens[4];
        VerbInfo31.Text = "ele " + tokens[5];
        VerbInfo41.Text = "nós " + tokens[6];
        VerbInfo51.Text = "vós " + tokens[7];
        VerbInfo61.Text = "eles " + tokens[8];

        VerbInfoH2.Text = "Pretérito Imperfeito";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];

        VerbInfoH3.Text = "Pretérito Perfeito";
        VerbInfo13.Text = tokens[15];
        VerbInfo23.Text = tokens[16];
        VerbInfo33.Text = tokens[17];
        VerbInfo43.Text = tokens[18];
        VerbInfo53.Text = tokens[19];
        VerbInfo63.Text = tokens[20];

        VerbInfoH4.Text = "Pretérito Mais-que-perfeito";
        VerbInfo71.Text = tokens[21];
        VerbInfo81.Text = tokens[22];
        VerbInfo91.Text = tokens[23];
        VerbInfo101.Text = tokens[24];
        VerbInfo111.Text = tokens[25];
        VerbInfo121.Text = tokens[26];

        VerbInfoH5.Text = "Futuro do Presente";
        VerbInfo72.Text = tokens[27];
        VerbInfo82.Text = tokens[28];
        VerbInfo92.Text = tokens[29];
        VerbInfo102.Text = tokens[30];
        VerbInfo112.Text = tokens[31];
        VerbInfo122.Text = tokens[32];

        VerbInfoH6.Text = "Futuro do Pretérito";
        VerbInfo73.Text = tokens[33];
        VerbInfo83.Text = tokens[34];
        VerbInfo93.Text = tokens[35];
        VerbInfo103.Text = tokens[36];
        VerbInfo113.Text = tokens[37];
        VerbInfo123.Text = tokens[38];

        ShowRow3(true);
        VerbInfoH7.Text = "Subjuntivo Presente";
        VerbInfo131.Text = "que eu " + tokens[39];
        VerbInfo141.Text = "que tu " + tokens[40];
        VerbInfo151.Text = "que ele " + tokens[41];
        VerbInfo161.Text = "que nós " + tokens[42];
        VerbInfo171.Text = "que vós " + tokens[43];
        VerbInfo181.Text = "que eles " + tokens[44];

        VerbInfoH8.Text = "Subjuntivo Futuro";
        VerbInfo132.Text = tokens[45];
        VerbInfo142.Text = tokens[46];
        VerbInfo152.Text = tokens[47];
        VerbInfo162.Text = tokens[48];
        VerbInfo172.Text = tokens[49];
        VerbInfo182.Text = tokens[50];

        VerbInfoH9.Text = "Imperativo";
        VerbInfo133.Text = "";
        VerbInfo143.Text = tokens[51] + " tu";
        VerbInfo153.Text = tokens[52] + " você";
        VerbInfo163.Text = tokens[53] + " nós";
        VerbInfo173.Text = tokens[54] + " vós";
        VerbInfo183.Text = tokens[55] + " vocês";
    }
    void LoadDeVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Partizip: " + tokens[1];
        Gerund.Text = "Gerundium: " + tokens[2];
        /* sein, gewesen, seiend, Präsens bin,bist,ist,sind,seid,sind,
         * Perfekt 9 bin gewesen,bist gewesen,ist gewesen,sind gewesen,seid gewesen,sind gewesen,
         * Plusquamperfekt 15 war gewesen,warst gewesen,war gewesen,waren gewesen,wart gewesen,waren gewesen,
         * Präteritum 21 war,warst,war,waren,wart,waren,
         * Präsens I 27 sei,seist,sei,seien,seiet,seien,
         * Präteritum II 33 wäre,wärest,wäre,wären,wäret,wären,
         * Imperative 39 Sei,Seien,Seid,Seien 
         sein, gewesen, seiend, bin,bist,ist,sind,seid,sind,
        bin gewesen,bist gewesen,ist gewesen,sind gewesen,seid gewesen,sind gewesen,
        war gewesen,warst gewesen,war gewesen,waren gewesen,wart gewesen,waren gewesen,
        war,warst,war,waren,wart,waren,
        sei,seist,sei,seien,seiet,seien,
        wäre,wärest,wäre,wären,wäret,wären,
        Sei, Seien, Seid, Seien*/
        VerbInfoH1.Text = "Präsens";
        VerbInfo11.Text = "ich " + tokens[3];
        VerbInfo21.Text = "du " + tokens[4];
        VerbInfo31.Text = "er/sie/es " + tokens[5];
        VerbInfo41.Text = "wir " + tokens[6];
        VerbInfo51.Text = "ihr " + tokens[7];
        VerbInfo61.Text = "sie/Sie " + tokens[8];

        VerbInfoH2.Text = "Perfekt";
        VerbInfo12.Text = tokens[9];
        VerbInfo22.Text = tokens[10];
        VerbInfo32.Text = tokens[11];
        VerbInfo42.Text = tokens[12];
        VerbInfo52.Text = tokens[13];
        VerbInfo62.Text = tokens[14];

        VerbInfoH3.Text = "Präteritum";
        VerbInfo13.Text = tokens[21];
        VerbInfo23.Text = tokens[22];
        VerbInfo33.Text = tokens[23];
        VerbInfo43.Text = tokens[24];
        VerbInfo53.Text = tokens[25];
        VerbInfo63.Text = tokens[26];

        VerbInfoH4.Text = "Plusquamperfekt";
        VerbInfo71.Text = tokens[15];
        VerbInfo81.Text = tokens[16];
        VerbInfo91.Text = tokens[17];
        VerbInfo101.Text = tokens[18];
        VerbInfo111.Text = tokens[19];
        VerbInfo121.Text = tokens[20];

        VerbInfoH5.Text = "Futur I";
        VerbInfo72.Text = "werde " + tokens[1];
        VerbInfo82.Text = "wirst " + tokens[1];
        VerbInfo92.Text = "wird " + tokens[1];
        VerbInfo102.Text = "werden " + tokens[1];
        VerbInfo112.Text = "werdet " + tokens[1];
        VerbInfo122.Text = "werden " + tokens[1];

        VerbInfoH6.Text = "Imperativ";
        VerbInfo73.Text = "";
        VerbInfo83.Text = tokens[39].Contains(" du") || tokens[39] == "-" ? tokens[39] : tokens[39] + " (du)!";
        VerbInfo93.Text = "";
        VerbInfo103.Text = tokens[40].Contains(" wir") || tokens[40] == "-" ? tokens[40] : tokens[40] + " wir!";
        VerbInfo113.Text = tokens[41].Contains(" ihr") || tokens[41] == "-" ? tokens[41] : tokens[41] + " ihr!";
        VerbInfo123.Text = tokens[42].Contains(" Sie") || tokens[42] == "-" ? tokens[42] : tokens[42] + " Sie!";
        ShowRow3(true);
        VerbInfoH7.Text = "Konj Präsens I";
        VerbInfo131.Text = "ich " + tokens[27];
        VerbInfo141.Text = "du " + tokens[28];
        VerbInfo151.Text = "er/sie/es " + tokens[29];
        VerbInfo161.Text = "wir " + tokens[30];
        VerbInfo171.Text = "ihr " + tokens[31];
        VerbInfo181.Text = "sie/Sie " + tokens[32];

        VerbInfoH8.Text = "Konj Präter II";
        VerbInfo132.Text = tokens[33];
        VerbInfo142.Text = tokens[34];
        VerbInfo152.Text = tokens[35];
        VerbInfo162.Text = tokens[36];
        VerbInfo172.Text = tokens[37];
        VerbInfo182.Text = tokens[38];

        VerbInfoH9.Text = "Konj Futur II";
        VerbInfo133.Text = "würde " + tokens[0];
        VerbInfo143.Text = "würdest " + tokens[0];
        VerbInfo153.Text = "würde " + tokens[0];
        VerbInfo163.Text = "würden " + tokens[0];
        VerbInfo173.Text = "würdet " + tokens[0];
        VerbInfo183.Text = "würden " + tokens[0];
    }

    void LoadRuVerbs(string[] tokens)
    {
        WordName.Text = tokens[0];
        Participle.Text = "Причастие: " + tokens[1];
        Gerund.Text = "Деепричастие: " + tokens[2];
        /* спорить, спорящий, споря, 
         * НАСТ 3 спорю,споришь,спорит,спорим,спорите,спорят,
         * ПРОШ 9 спорил/спорила,спорил/спорила,спорил/спорила,спорили,спорили,спорили, 
         * БУД 15 буду спорить,будешь спорить,будет спорить,будем спорить,будете спорить,будут спорить, 
         * УСЛ 21 бы спорил / спорила,бы спорил / спорила,бы спорил / спорила,бы спорили,бы спорили,бы спорили,
         * ПОВ 27 спорь,спорьте
 */
        VerbInfoH1.Text = "Настоящее время";
        VerbInfo11.Text = "Я " + tokens[3];
        VerbInfo21.Text = "Ты " + tokens[4];
        VerbInfo31.Text = "Он/она " + tokens[5];
        VerbInfo41.Text = "Мы " + tokens[6];
        VerbInfo51.Text = "Вы " + tokens[7];
        VerbInfo61.Text = "Они " + tokens[8];

        VerbInfoH2.Text = "Прошедшее Муж.";
        VerbInfo12.Text = tokens[9].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo22.Text = tokens[10].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo32.Text = tokens[11].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo42.Text = tokens[12].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo52.Text = tokens[13].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        VerbInfo62.Text = tokens[14].Split('/', StringSplitOptions.RemoveEmptyEntries).First().Trim();

        VerbInfoH3.Text = "Прошедшее Жен.";
        VerbInfo13.Text = tokens[9].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo23.Text = tokens[10].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo33.Text = tokens[11].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo43.Text = tokens[12].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo53.Text = tokens[13].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();
        VerbInfo63.Text = tokens[14].Split('/', StringSplitOptions.RemoveEmptyEntries).Last().Trim();

        VerbInfoH4.Text = "Будущее время";
        VerbInfo71.Text = "Я " + tokens[15];
        VerbInfo81.Text = "Ты " + tokens[16];
        VerbInfo91.Text = "Он/она " + tokens[17];
        VerbInfo101.Text = "Мы " + tokens[18];
        VerbInfo111.Text = "Вы " + tokens[19];
        VerbInfo121.Text = "Они " + tokens[20];

        VerbInfoH5.Text = "Сослагательное";
        VerbInfo72.Text = tokens[21];
        VerbInfo82.Text = tokens[22];
        VerbInfo92.Text = tokens[23];
        VerbInfo102.Text = tokens[24];
        VerbInfo112.Text = tokens[25];
        VerbInfo122.Text = tokens[26];

        VerbInfoH6.Text = "Повелительное";
        VerbInfo73.Text = "";
        VerbInfo83.Text = tokens[27];
        VerbInfo93.Text = "";
        VerbInfo103.Text = "";
        VerbInfo113.Text = tokens[28];
        VerbInfo123.Text = "";
        ShowRow3(false);
    }
}
