using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Localization;
using GameLocale=UnityEngine.Localization.Locale;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using TMPro;

[BepInPlugin("skarig.silverpines.localization", "Silver Pines Ukrainian Localization", "0.1.9")]
public class LocalizationPlugin : BasePlugin {
 internal static LocalizationPlugin Self;
 internal static ConfigEntry<bool> TextureChoice,TextChoice;
 internal static bool TextAtStartup,TexturesAtStartup,Initialized,Building,StartupLocaleApplied;
 internal static string Root;
 internal static GameLocale Ukrainian;
 internal static Dictionary<string,List<TextRow>> Texts;
 internal static Dictionary<string,BundleRow> Bundles;
 internal static readonly List<StringTable> Tables=new List<StringTable>();
 internal static ResourceLocationMap UkrainianCatalog;
 internal static readonly List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<StringTable>> SourceHandles=new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<StringTable>>();
 internal static readonly List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<StringTable>> Handles=new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<StringTable>>();
 internal static readonly HashSet<string> Logged=new HashSet<string>();
 internal static readonly Dictionary<string,bool> Verified=new Dictionary<string,bool>();
 public override void Load(){
  Self=this;Root=Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
  TextureChoice=Config.Bind("Localization","UkrainianTextures",true,"Ukrainian world textures. Change in the main menu; restart the game to apply.");
  TextChoice=Config.Bind("Localization","UkrainianText",true,"Last selected text locale. Ukrainian is added separately; original locales stay intact.");
  TextAtStartup=TextChoice.Value;TexturesAtStartup=TextureChoice.Value;
  Texts=JsonSerializer.Deserialize<Dictionary<string,List<TextRow>>>(File.ReadAllText(Path.Combine(Root,"texts.json")));
  Bundles=JsonSerializer.Deserialize<List<BundleRow>>(File.ReadAllText(Path.Combine(Root,"bundles.json"))).ToDictionary(x=>x.name,StringComparer.OrdinalIgnoreCase);
  new Harmony("skarig.silverpines.localization").PatchAll();
  AddComponent<LocalizationController>();
  Log.LogInfo("Sidecar localization loaded. No stock files or stock Russian string tables are replaced. Texture changes require restart.");
 }
 internal static bool CanChangeTextures(){
  foreach(var s in Resources.FindObjectsOfTypeAll<GameState>())if(s.IsInState(GameState.GameStateFlag.GameActive))return false;
  bool menu=false;
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){
   var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
   if(s.isLoaded&&s.name=="GameManagers")return false;
   if(s.isLoaded&&(s.name=="SystemMenu"||s.name.IndexOf("MainMenu",StringComparison.OrdinalIgnoreCase)>=0))menu=true;
  }
  return menu;
 }
 internal static string HashFile(string p){using(var stream=File.OpenRead(p))using(var sha=SHA256.Create())return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();}
 internal static string ResolveBundle(string path){
  if(string.IsNullOrEmpty(path))return null;
  string local=path.StartsWith("file:",StringComparison.OrdinalIgnoreCase)?new Uri(path).LocalPath:path;
  if(!Bundles.TryGetValue(Path.GetFileName(local),out var b))return null;
  // Only redirect installed game resources, never arbitrary files sharing a filename.
  string expected=Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,"aa","StandaloneWindows64",b.name));
  if(!string.Equals(Path.GetFullPath(local),expected,StringComparison.OrdinalIgnoreCase))return null;
  string hash=null;
  if(b.role=="font")hash=b.full;
  else if(b.role=="interface"&&b.name.IndexOf("mainmenu",StringComparison.OrdinalIgnoreCase)>=0)hash=TexturesAtStartup?b.full:null;
  else if(b.role=="interface")hash=TextAtStartup?(TexturesAtStartup?b.full:b.light):null;
  else if(TexturesAtStartup)hash=b.full;
  else if(TextAtStartup)hash=b.light;
  if(string.IsNullOrEmpty(hash))return null;
  string replacement=Path.Combine(Root,"Bundles",hash+".bundle");
  if(!Verified.TryGetValue(b.name,out bool valid)){
   valid=File.Exists(replacement)&&HashFile(expected)==b.baseline&&HashFile(replacement)==hash;
   Verified[b.name]=valid;
   if(!valid)Self.Log.LogWarning("Overlay skipped: missing/changed game resource or damaged sidecar: "+b.name+". Original resource kept; rebuild overlay for this game update.");
  }
  if(!valid)return null;
  if(Logged.Add(b.name))Self.Log.LogInfo("Localized resource: "+b.name);
  return replacement;
 }
 internal static bool BuildLocale(){
  if(Initialized)return true;
  if(Building||!LocalizationSettings.InitializationOperation.IsDone)return false;
  Building=true;
  try{
   var locales=LocalizationSettings.AvailableLocales;
   var en=locales.GetLocale(new LocaleIdentifier("en"));
   var ru=locales.GetLocale(new LocaleIdentifier("ru"));
   if(en==null)throw new Exception("English locale not ready");
   Ukrainian=GameLocale.CreateLocale("uk");Ukrainian.LocaleName="Українська";Ukrainian.name="Ukrainian (Skarig)";
   Ukrainian.Metadata.AddMetadata(new FallbackLocale(ru??en).Cast<IMetadata>());
   UnityEngine.Object.DontDestroyOnLoad(Ukrainian);
   var db=LocalizationSettings.StringDatabase;
   // Hold stock shared-data dependencies, then load a real copy of the native text
   // pack. Only locale identifiers and approved text values differ in that copy.
   if(SourceHandles.Count==0)foreach(var pair in Texts){
    var source=db.GetTableAsync((TableReference)pair.Key,en);
    if(source.WaitForCompletion()==null)throw new Exception("English table unavailable: "+pair.Key);
    UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle retainedSource=source;
    Addressables.ResourceManager.Acquire(retainedSource);SourceHandles.Add(source);
   }
   RegisterNativeCatalog(db,en);
   if(Tables.Count==0)foreach(var pair in Texts){
    var handle=Addressables.LoadAssetAsync<StringTable>(AddressKey(pair.Key+"_uk"));
    var table=handle.WaitForCompletion();
    if(table==null)throw new Exception("Native Ukrainian asset loading failed: "+pair.Key);
    Tables.Add(table);Handles.Add(handle);
   }
   if(Tables.Count!=24)throw new Exception("Native Ukrainian text pack table count: "+Tables.Count);
   foreach(var table in Tables){
    var name=table.SharedData.TableCollectionName;
    if(table.LocaleIdentifier.Code!="uk")throw new Exception("Wrong locale in copied text pack: "+name);
    Self.Log.LogInfo("Copied native Ukrainian table: "+name+", entries="+table.Count);
   }
   var menu=Tables.FirstOrDefault(t=>t.SharedData.TableCollectionName=="SystemMenu");
   foreach(var key in new[]{"New Game","LoadPreviousSave","Change Profile","Options","Credits","Quit"}){
    var entry=menu?.GetEntry(key);
    if(entry==null||string.IsNullOrEmpty(entry.Value))throw new Exception("Ukrainian preflight failed for SystemMenu/"+key);
    Self.Log.LogInfo("Ukrainian menu preflight: "+key+" = "+entry.Value);
   }
   // Exercise the unmodified Unity lookup before making the locale selectable.
   foreach(var table in Tables){
    var name=table.SharedData.TableCollectionName;
    var byName=db.GetTableAsync((TableReference)name,Ukrainian).WaitForCompletion();
    var byGuid=db.GetTableAsync((TableReference)table.SharedData.TableCollectionNameGuid,Ukrainian).WaitForCompletion();
    if(byName==null||byGuid==null||byName.LocaleIdentifier.Code!="uk"||byGuid.LocaleIdentifier.Code!="uk")throw new Exception("Native catalog preflight failed: "+name);
   }
   var actualMenu=db.GetTableAsync((TableReference)"SystemMenu",Ukrainian).WaitForCompletion();
   foreach(var key in new[]{"New Game","LoadPreviousSave","Change Profile","Options","Credits","Quit"})if(actualMenu.GetEntry(key)?.Value!=menu.GetEntry(key).Value)throw new Exception("Native menu text mismatch: "+key);
   Self.Log.LogInfo("NATIVE CATALOG PREFLIGHT PASSED: all 24 tables resolve by name and GUID; six main-menu values match.");
   locales.AddLocale(Ukrainian);Initialized=true;
   Self.Log.LogInfo("Registered independent Ukrainian locale: "+Tables.Count+" tables. Russian locale unchanged.");
   return true;
  }catch(Exception e){Self.Log.LogError("Ukrainian locale initialization failed: "+e);return false;}
  finally{Building=false;}
 }
 internal static bool IsUkrainian=>Initialized&&LocalizationSettings.SelectedLocale?.Identifier.Code=="uk";
 static Il2CppSystem.Object AddressKey(string value)=>(Il2CppSystem.String)value;
 static int LocationCount(Il2CppSystem.Collections.Generic.IList<IResourceLocation> list)=>list==null?0:list.Cast<Il2CppSystem.Collections.Generic.ICollection<IResourceLocation>>().Count;
 static IResourceLocation FindEnglishLocation(string name){
  var enumerator=Addressables.ResourceLocators.GetEnumerator().Cast<Il2CppSystem.Collections.IEnumerator>();
  while(enumerator.MoveNext()){
   var locator=enumerator.Current.Cast<IResourceLocator>();
   if(locator.Locate(AddressKey(name+"_en"),Il2CppType.Of<StringTable>(),out var found)&&LocationCount(found)>0)return found[0];
  }
  throw new Exception("Stock English addressable location not found: "+name);
 }
 static void RegisterNativeCatalog(LocalizedStringDatabase db,GameLocale en){
  if(UkrainianCatalog!=null)return;
  var paths=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(Root,"uk_asset_paths.json")));
  var catalog=new ResourceLocationMap("Skarig.Ukrainian.NativeTables",100);
  IResourceLocation ukBundle=null;
  foreach(var pair in Texts){
   var original=FindEnglishLocation(pair.Key);
   var deps=new List<IResourceLocation>();
   for(int i=0;i<LocationCount(original.Dependencies);i++){
    var dep=original.Dependencies[i];
    if(dep.InternalId.IndexOf("localization-string-tables-english(en)",StringComparison.OrdinalIgnoreCase)>=0){
     if(ukBundle==null){
      var inherited=new List<IResourceLocation>();for(int j=0;j<LocationCount(dep.Dependencies);j++)inherited.Add(dep.Dependencies[j]);
      var location=new ResourceLocationBase("Skarig.Ukrainian.TextBundle",Path.Combine(Root,"UkrainianTexts.bundle"),dep.ProviderId,dep.ResourceType,inherited.ToArray());
      var options=new AssetBundleRequestOptions();options.BundleName="skarig.uk.texts.bundle";options.Crc=0;options.Hash="";options.BundleSize=new FileInfo(Path.Combine(Root,"UkrainianTexts.bundle")).Length;options.UseUnityWebRequestForLocalBundles=false;
      location.Data=options;ukBundle=location.Cast<IResourceLocation>();
     }
     deps.Add(ukBundle);
    }else deps.Add(dep);
   }
   if(ukBundle==null||!deps.Contains(ukBundle))throw new Exception("English table bundle dependency was not identified: "+pair.Key);
   var asset=new ResourceLocationBase(pair.Key+"_uk",paths[pair.Key],original.ProviderId,original.ResourceType,deps.ToArray());asset.Data=original.Data;
   var resource=asset.Cast<IResourceLocation>();
   catalog.Add(AddressKey(pair.Key+"_uk"),resource);
   catalog.Add(AddressKey("Locale-uk"),resource);
   var shared=db.GetTable((TableReference)pair.Key,en).SharedData;
   var guid=shared.TableCollectionNameGuid.ToString("N");
   // Only the localization system's qualified label, not generic asset/scene
   // labels such as "Levels" or "Video", and not shared-data asset addresses.
   catalog.Add(AddressKey("GUID:"+guid),resource);
  }
  Addressables.AddResourceLocator(catalog.Cast<IResourceLocator>(),null,null);UkrainianCatalog=catalog;
  Self.Log.LogInfo("Native Ukrainian Addressables catalog registered: 24 table locations; stock catalogs unchanged. No generic method detours.");
 }
 internal static StringTable FindTable(TableReference reference){
  if(!Initialized)return null;
  // Empty LocalizedString references are legal in Carpenter: its helper uses
  // the accompanying raw string. Do not Validate an absent optional reference.
  if(string.IsNullOrEmpty(reference.m_TableCollectionName)&&reference._ReferenceType_k__BackingField==TableReference.Type.Empty)return null;
  try{
  reference.Validate();
  foreach(var t in Tables){
   if(reference.ReferenceType==TableReference.Type.Name&&reference.TableCollectionName==t.SharedData.TableCollectionName)return t;
   if(reference.ReferenceType==TableReference.Type.Guid&&reference.TableCollectionNameGuid.Equals(t.SharedData.TableCollectionNameGuid))return t;
  }
  return null;
  }catch(Exception){return null;}
 }
 internal static StringTableEntry ResolveEntry(StringTable table,TableEntryReference reference){
  if(table==null||(reference.m_KeyId==0&&string.IsNullOrEmpty(reference.m_Key)))return null;
  try{
  reference.Validate();
  return reference.ReferenceType==TableEntryReference.Type.Id?table.GetEntry(reference.KeyId):table.GetEntry(reference.Key);
  }catch(Exception){return null;}
 }
 internal static void EnsureTables(){
  // The native locator owns lookup; retained LoadAsset handles own asset lifetime.
 }
 internal static string TranslateLiteral(string value){
  if(!IsUkrainian||value==null)return value;
  // Calling status includes the dialed digits on a second line in some phones.
  // Translate only its heading, retaining number, whitespace and line breaks.
  if(System.Text.RegularExpressions.Regex.IsMatch(value,@"^\s*DIALING(?=[\s.\u2026<\d]|$)",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
   return System.Text.RegularExpressions.Regex.Replace(value,@"\bDIALING\b","ВИКЛИК",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  string trimmed=value.Trim();string replacement=null;
  switch(trimmed.ToUpperInvariant()){
   case "INSERT COIN":replacement="ВСТАВ МОНЕТУ";break;
   case "INSERT COINS":replacement="ВСТАВТЕ МОНЕТИ";break;
   case "DIAL NUMBER":replacement="НАБЕРИ НОМЕР";break;
   case "DIALING":replacement="ВИКЛИК";break;
   case "ERROR":replacement="ПОМИЛКА";break;
   case "INSUFFICIENT CREDIT":replacement="НЕДОСТАТНЬО КОШТІВ";break;
   case "SELECTED":replacement="ВИБРАНО";break;
   case "THANK YOU":replacement="ДЯКУЄМО";break;
  }
  if(replacement!=null){int start=0;while(start<value.Length&&char.IsWhiteSpace(value[start]))start++;return value.Substring(0,start)+replacement+value.Substring(start+trimmed.Length);}
  switch(value){
   case "INSERT COIN":return "ВСТАВ МОНЕТУ";
   case "INSERT COINS":return "ВСТАВТЕ МОНЕТИ";
   case "INSERT COINS   INSERT COINS   ":return "ВСТАВТЕ МОНЕТИ   ВСТАВТЕ МОНЕТИ   ";
   case "DIAL NUMBER":return "НАБЕРИ НОМЕР";
   case "DIALING\n":return "ВИКЛИК\n";
   case "Error ":return "Помилка ";
   case "Error":case "ERROR":return "ПОМИЛКА";
   case "Insufficient Credit ":return "Недостатньо коштів ";
   case "Insufficient Credit":case "INSUFFICIENT CREDIT":return "НЕДОСТАТНЬО КОШТІВ";
   case " Selected ":return " Вибрано ";
   default:return value;
  }
 }
 public class TextRow{public string id{get;set;}public string key{get;set;}public string text{get;set;}}
 public class BundleRow{public string name{get;set;}public string full{get;set;}public string light{get;set;}public string role{get;set;}public string baseline{get;set;}public string native_name{get;set;}}
}

// Redirect the Addressables provider before it opens the bundle. CRC is changed only
// on the exact redirected resource; catalog bytes and unrelated requests stay intact.
[HarmonyPatch]
static class LocalizedBundlePatch{
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.GetDeclaredMethods(typeof(AssetBundleResource)).First(m=>m.Name=="GetLoadInfo"&&m.GetParameters().Length==4&&m.GetParameters()[0].ParameterType==typeof(IResourceLocation));
 static void Postfix(IResourceLocation __0,ref AssetBundleResource.LoadType __2,ref string __3){
  try{var path=LocalizationPlugin.ResolveBundle(__3);if(path==null)return;
   var options=__0.Data?.TryCast<AssetBundleRequestOptions>();if(options==null){LocalizationPlugin.Self.Log.LogWarning("Overlay request lacks bundle options; original retained.");return;}
   options.Crc=0;options.UseUnityWebRequestForLocalBundles=false;
   __2=AssetBundleResource.LoadType.Local;__3=path;
  }catch(Exception e){LocalizationPlugin.Self.Log.LogError("Overlay routing error: "+e.Message);}
 }
}

[HarmonyPatch]
static class LocalizedBundleHandlePatch{
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.GetDeclaredMethods(typeof(AssetBundleResource)).First(m=>m.Name=="GetLoadInfo"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(ProvideHandle));
 static void Postfix(ProvideHandle __0,ref AssetBundleResource.LoadType __1,ref string __2){
  try{var path=LocalizationPlugin.ResolveBundle(__2);if(path==null)return;
   var options=__0.Location.Data?.TryCast<AssetBundleRequestOptions>();if(options==null)return;
   options.Crc=0;options.UseUnityWebRequestForLocalBundles=false;__1=AssetBundleResource.LoadType.Local;__2=path;
  }catch(Exception e){LocalizationPlugin.Self.Log.LogError("Overlay handle routing error: "+e.Message);}
 }
}

[HarmonyPatch(typeof(OptionsTabSystem),nameof(OptionsTabSystem.PopulateOptionsTab))]
static class LocalizationOptionsPatch{
 static void Prefix(){LocalizationPlugin.BuildLocale();}
 static void Postfix(OptionsTabSystem __instance){
  try{
   if(__instance.m_optionsListContainer.Find("SkarigTextureLanguage")!=null)return;
   var choices=new Il2CppSystem.Collections.Generic.List<string>();choices.Add("Оригінальні");choices.Add("Українські");
   var action=DelegateSupport.ConvertDelegate<UnityAction<int>>(new Action<int>(v=>{
    if(!LocalizationPlugin.CanChangeTextures())return;
    LocalizationPlugin.TextureChoice.Value=v==1;LocalizationPlugin.TextureChoice.ConfigFile.Save();LocalizationController.Refresh();
   }));
   var cycle=__instance.AddCycleButtonOption("Language",choices,LocalizationPlugin.TextureChoice.Value?1:0,action,__instance.GetLabelString("Language"),null);
   var container=__instance.m_optionsListContainer;var row=container.GetChild(container.childCount-1).gameObject;row.name="SkarigTextureLanguage";
   if(cycle.m_labelLocalizeString!=null)cycle.m_labelLocalizeString.enabled=false;
   LocalizationController.Rows.Add(Tuple.Create(cycle,row));LocalizationController.Refresh();
  }catch(Exception e){LocalizationPlugin.Self.Log.LogError(e);}
 }
}
[HarmonyPatch(typeof(OptionsTooltipInfoPanel),nameof(OptionsTooltipInfoPanel.ShowToolTip))]
static class LocalizationTooltipPatch{
 static bool Prefix(OptionsTooltipInfoPanel __instance,OptionsTooltipElement __0){
  if(__0==null)return true;var tr=__0.transform;
  while(tr!=null){if(tr.gameObject.name=="SkarigTextureLanguage"){
   __instance.m_activeTooltip=__0;__instance.m_container.SetActive(true);
   __instance.m_text.text="Набір текстур можна вибрати лише в головному меню. Щоб застосувати зміну, повністю перезапустіть гру. Оригінальні файли гри залишаються незмінними.";
   return false;
  }tr=tr.parent;}return true;
 }
}
[HarmonyPatch(typeof(PhoneMinigame),"get_StringToShow")]
static class PhoneLiteralPatch{static void Postfix(ref string __result){__result=LocalizationPlugin.TranslateLiteral(__result);}}
[HarmonyPatch(typeof(VendingMachineMinigame),"get_StringToShow")]
static class VendingLiteralPatch{static void Postfix(ref string __result){__result=LocalizationPlugin.TranslateLiteral(__result);}}

// Supply localized source strings before the native vending loop slices them.
// Translating a visible substring after scrolling would break word boundaries.
[HarmonyPatch(typeof(VendingMachineMinigame),nameof(VendingMachineMinigame.OnEnable))]
static class VendingLCDSourcePatch{
 static void Prefix(VendingMachineMinigame __instance){
  if(!LocalizationPlugin.IsUkrainian)return;
  try{
   __instance.m_idleDisplayString=new LocalizedString((TableReference)"Minigames",(TableEntryReference)"VendingMachine_AttractText");
   __instance.m_insufficientCreditString=new LocalizedString((TableReference)"Minigames",(TableEntryReference)"VendingMachine_InsufficientCredit");
   __instance.m_invalidEntryString=new LocalizedString((TableReference)"Minigames",(TableEntryReference)"VendingMachine_InvalidEntry");
   __instance.m_succesfulPurchaseString=new LocalizedString((TableReference)"Minigames",(TableEntryReference)"VendingMachine_SuccesfulPurchase");
  }catch(Exception e){LocalizationPlugin.Self.Log.LogWarning("Vending LCD source: "+e.Message);}
 }
}

// IL2CPP can inline the StringToShow getter inside UpdateLCDText. Use the actual
// display boundary as well, retaining the native blink's empty frames and digits.
[HarmonyPatch(typeof(PhoneMinigame),nameof(PhoneMinigame.UpdateLCDText))]
static class PhoneLCDDisplayPatch{
 static void Postfix(PhoneMinigame __instance){
  if(!LocalizationPlugin.IsUkrainian)return;
  try{
   var display=__instance.m_codeDisplay;if(display==null||string.IsNullOrWhiteSpace(display.text))return;
   var source=__instance.m_stringToShow;var translated=LocalizationPlugin.TranslateLiteral(source);
   if(source!=translated)display.text=translated.Trim();
   else{var rendered=LocalizationPlugin.TranslateLiteral(display.text);if(rendered!=display.text)display.text=rendered;}
  }catch(Exception e){LocalizationPlugin.Self.Log.LogWarning("Phone LCD display: "+e.Message);}
 }
}

// Dialing writes directly from its coroutine instead of using UpdateLCDText.
// Translate after each native step, retaining waits, number and call handling.
[HarmonyPatch(typeof(PhoneMinigame._Dialing_d__81),nameof(PhoneMinigame._Dialing_d__81.MoveNext))]
static class PhoneDialingDisplayPatch{
 static void Postfix(PhoneMinigame._Dialing_d__81 __instance){
  if(!LocalizationPlugin.IsUkrainian)return;
  try{
   var display=__instance.__4__this?.m_codeDisplay;if(display==null)return;
   var translated=LocalizationPlugin.TranslateLiteral(display.text);
   if(translated!=display.text){display.text=translated;LocalizationPlugin.Self.Log.LogInfo("Phone dialing heading localized; dialed digits preserved.");}
  }catch(Exception e){LocalizationPlugin.Self.Log.LogWarning("Dialing LCD: "+e.Message);}
 }
}

[HarmonyPatch(typeof(MinigameHoverText),nameof(MinigameHoverText.OnEnable))]
static class LocalizedTextureHoverPatch{
 static bool Prefix(MinigameHoverText __instance){if(!LocalizationPlugin.TexturesAtStartup)return true;__instance.enabled=false;return false;}
}

[HarmonyPatch(typeof(LocalizationSettings),nameof(LocalizationSettings.SetSelectedLocale))]
static class UkrainianLocaleSelectionPatch{
 static void Prefix(ref GameLocale __0){
  if(LocalizationPlugin.Initialized&&!LocalizationPlugin.StartupLocaleApplied&&LocalizationPlugin.TextAtStartup)__0=LocalizationPlugin.Ukrainian;
  if(__0?.Identifier.Code=="uk")LocalizationPlugin.EnsureTables();
 }
}

// Concrete UI lifecycle hooks, never a detour of a shared IL2CPP generic body.
// Resolve the ready Cyrillic font before TMP can build its first visible mesh.
[HarmonyPatch(typeof(TextMeshProUGUI),"OnEnable")]
static class UkrainianFontBeforeDisplayPatch{
 static void Prefix(TextMeshProUGUI __instance){
  try{LocalizationController.PrepareFont(__instance.GetComponent<LocalizeTMPFontEvent>());}
  catch(Exception e){LocalizationPlugin.Self.Log.LogWarning("Font preparation: "+e.Message);}
 }
}

[HarmonyPatch(typeof(BuildInfoDisplay),nameof(BuildInfoDisplay.Awake))]
static class ActualBuildDisplayPatch{
 static void Postfix(BuildInfoDisplay __instance){
  try{
   var text=__instance.m_text;if(text==null)return;
   LocalizationPlugin.Self.Log.LogInfo("Stock build metadata: label="+text.text+", number="+BuildVersion.s_buildNumber+", appVersion="+Application.version+", teamCity="+BuildVersion.s_teamCityBuild);
   if(text.text==null||text.text.IndexOf("Local Build",StringComparison.OrdinalIgnoreCase)<0)return;
   string version=BuildVersion.s_buildNumber;
   if(string.IsNullOrWhiteSpace(version)||version.IndexOf("local",StringComparison.OrdinalIgnoreCase)>=0||version=="0")version=Application.version;
   if(!string.IsNullOrWhiteSpace(version)&&version!="1"&&version!="1.0"&&version!="0.1"&&version!="0.0.0"&&version.IndexOf("local",StringComparison.OrdinalIgnoreCase)<0){text.text="v"+version;return;}
   // Honest fallback to the installed Steam build, never an invented game version.
   var steamapps=Directory.GetParent(Application.dataPath)?.Parent?.Parent?.FullName;
   var manifest=Path.Combine(steamapps??"","appmanifest_2333000.acf");
   if(File.Exists(manifest)){
    var match=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(manifest),"\"buildid\"\\s+\"([0-9]+)\"");
    if(match.Success)text.text="Steam build "+match.Groups[1].Value;
   }
  }catch(Exception e){LocalizationPlugin.Self.Log.LogWarning("Build label: "+e.Message);}
 }
}

// Locale changes can discard cache aliases. Re-establish them before a string request.
[HarmonyPatch(typeof(LocalizedStringDatabase),nameof(LocalizedStringDatabase.GetLocalizedStringAsyncInternal))]
static class UkrainianStringRequestPatch{
 static void Prefix(GameLocale __3){if(LocalizationPlugin.Initialized&&(__3?.Identifier.Code=="uk"||(__3==null&&LocalizationPlugin.IsUkrainian)))LocalizationPlugin.EnsureTables();}
}

// Also recover an already-cached missing entry at the common formatter boundary.
// The stock formatter still handles smart strings, arguments and escaping.
[HarmonyPatch(typeof(LocalizedStringDatabase),nameof(LocalizedStringDatabase.GenerateLocalizedString))]
static class UkrainianStringFormatPatch{
 static void Prefix(ref StringTable __0,ref StringTableEntry __1,TableReference __2,TableEntryReference __3,ref GameLocale __4){
  try{
  if(!LocalizationPlugin.Initialized||!(__4?.Identifier.Code=="uk"||(__4==null&&LocalizationPlugin.IsUkrainian)))return;
  var table=LocalizationPlugin.FindTable(__2);if(table==null)return;
  var entry=LocalizationPlugin.ResolveEntry(table,__3);
  if(entry!=null){__0=table;__1=entry;__4=LocalizationPlugin.Ukrainian;}
  }catch(Exception e){LocalizationPlugin.Self.Log.LogWarning("Native string formatter retained: "+e.Message);}
 }
}

// Carpenter's synchronous helper can reject a locale before Unity's async formatter
// is reached. Supply the retained Ukrainian entry at that actual game boundary.
[HarmonyPatch(typeof(LocalizationUtility),nameof(LocalizationUtility.GetLocalizedString))]
static class UkrainianGameStringPatch{
 static readonly HashSet<string> Seen=new HashSet<string>();
 static bool Prefix(LocalizedString __0,ref string __result){
  try{
  if(!LocalizationPlugin.IsUkrainian||__0==null)return true;
  var table=LocalizationPlugin.FindTable(__0.TableReference);if(table==null)return true;
  var entry=LocalizationPlugin.ResolveEntry(table,__0.TableEntryReference);if(entry==null)return true;
  __result=LocalizationSettings.StringDatabase.GenerateLocalizedString(table,entry,__0.TableReference,__0.TableEntryReference,LocalizationPlugin.Ukrainian,__0.Arguments);
  if(Seen.Count<12&&Seen.Add(table.SharedData.TableCollectionName+"/"+entry.Key))LocalizationPlugin.Self.Log.LogInfo("Game localization bridge: "+table.SharedData.TableCollectionName+"/"+entry.Key+" = "+__result);
  return false;
  }catch(Exception e){
   // A translation failure must never abort the caller's examine/close/input
   // setup. Original helper retains raw-string fallback and error handling.
   LocalizationPlugin.Self.Log.LogWarning("Original game string helper retained: "+e.Message);
   return true;
  }
 }
}

public class LocalizationController:MonoBehaviour{
 public LocalizationController(IntPtr p):base(p){}
 internal static readonly List<Tuple<CycleButton,GameObject>> Rows=new List<Tuple<CycleButton,GameObject>>();
 sealed class FontBinding{internal LocalizeTMPFontEvent Event;internal LocalizedTMPFont Reference;internal GameLocale Original;internal string LastFont;}
 static readonly Dictionary<int,FontBinding> FontBindings=new Dictionary<int,FontBinding>();
 static readonly Dictionary<string,TMP_FontAsset> ReadyFonts=new Dictionary<string,TMP_FontAsset>();
 static readonly List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle> FontHandles=new List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle>();
 static readonly Dictionary<string,LocalizedTMPFont> PendingFonts=new Dictionary<string,LocalizedTMPFont>();
 static bool fontsQueued;
 static string FontKey(LocalizedTMPFont reference)=>reference.TableReference.ToString()+"/"+reference.TableEntryReference.ToString();
 static void QueueFont(LocalizedTMPFont reference){
  string key=FontKey(reference);
  if(ReadyFonts.ContainsKey(key)||PendingFonts.ContainsKey(key))return;
  var loader=new LocalizedTMPFont();loader.TableReference=reference.TableReference;loader.TableEntryReference=reference.TableEntryReference;
  loader.LocaleOverride=LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("ru"));loader.WaitForCompletion=false;
  loader.LoadAssetAsync();PendingFonts[key]=loader;
 }
 static void WarmFonts(){
  if(!fontsQueued){
   fontsQueued=true;
   foreach(long id in new long[]{28473634816,1756849844224,337126422556672,2968348080074752}){
    var reference=new LocalizedTMPFont();reference.TableReference=(TableReference)"Fonts";reference.TableEntryReference=(TableEntryReference)id;QueueFont(reference);
   }
  }
  foreach(var pair in PendingFonts.ToArray()){
   var handle=pair.Value.CurrentLoadingOperationHandle;
   if(!handle.IsValid()||!handle.IsDone)continue;
   if(handle.Result!=null){
    UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle retained=handle;Addressables.ResourceManager.Acquire(retained);FontHandles.Add(retained);ReadyFonts[pair.Key]=handle.Result;
   }
   PendingFonts.Remove(pair.Key);
  }
 }
 internal static void PrepareStartupLocale(){
  if(LocalizationPlugin.Building||!LocalizationSettings.HasSettings)return;
  if(!LocalizationPlugin.Initialized&&!LocalizationPlugin.BuildLocale())return;
  if(!LocalizationPlugin.StartupLocaleApplied&&LocalizationPlugin.TextAtStartup&&LocalizationSettings.SelectedLocale!=LocalizationPlugin.Ukrainian)
   LocalizationSettings.SelectedLocale=LocalizationPlugin.Ukrainian;
 }
 float next,retry;bool selected,bridgeChecked;
 public void Update(){
  PrepareStartupLocale();
  if(Time.unscaledTime<next)return;next=Time.unscaledTime+.5f;
  if(!LocalizationPlugin.Initialized&&Time.unscaledTime>=retry){retry=Time.unscaledTime+10;LocalizationPlugin.BuildLocale();}
  if(LocalizationPlugin.Initialized){
   WarmFonts();
   bool ua=LocalizationPlugin.IsUkrainian;
   if(!selected&&LocalizationPlugin.CanChangeTextures()){
    selected=true;LocalizationPlugin.StartupLocaleApplied=true;if(LocalizationPlugin.TextChoice.Value)LocalizationSettings.SelectedLocale=LocalizationPlugin.Ukrainian;
   }
   else if(selected&&ua!=LocalizationPlugin.TextChoice.Value){LocalizationPlugin.TextChoice.Value=ua;LocalizationPlugin.TextChoice.ConfigFile.Save();}
   if(LocalizationPlugin.IsUkrainian&&!bridgeChecked){
    bridgeChecked=true;
    var value=LocalizationUtility.GetLocalizedString(new LocalizedString((TableReference)"SystemMenu",(TableEntryReference)"Options"),"Options",null);
    LocalizationPlugin.Self.Log.LogInfo("ACTUAL GAME LOCALIZATION SELF-TEST: Options = "+value);
    var native=LocalizationSettings.StringDatabase.GetTableAsync((TableReference)"SystemMenu",LocalizationPlugin.Ukrainian).WaitForCompletion();
    LocalizationPlugin.Self.Log.LogInfo("NATIVE ASYNC TABLE SELF-TEST: New Game = "+native?.GetEntry("New Game")?.Value);
   }
   ApplyOriginalFontLocales();
  }
  Refresh();
 }
 static void ApplyOriginalFontLocales(){
  foreach(var component in Resources.FindObjectsOfTypeAll<LocalizeTMPFontEvent>())if(component!=null&&component.gameObject.scene.IsValid())PrepareFont(component);
  foreach(var id in FontBindings.Where(p=>p.Value.Event==null).Select(p=>p.Key).ToArray())FontBindings.Remove(id);
 }
 internal static void PrepareFont(LocalizeTMPFontEvent component){
  if(component==null||!LocalizationPlugin.Initialized)return;
  bool ua=LocalizationPlugin.IsUkrainian;
  var ru=LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("ru"));if(ru==null)return;
   var reference=component.AssetReference;if(reference==null||reference.IsEmpty)return;
   int id=component.GetInstanceID();
   bool customRow=false;var parent=component.transform;
   while(parent!=null){if(parent.gameObject.name=="SkarigTextureLanguage"||parent.gameObject.name=="SkarigVoiceLanguage"){customRow=true;break;}parent=parent.parent;}
   bool useCyrillic=ua||customRow;
   if(!FontBindings.TryGetValue(id,out var binding)){
    if(!useCyrillic)return;
    binding=new FontBinding{Event=component,Reference=reference,Original=reference.LocaleOverride};FontBindings[id]=binding;
   }
   var desired=useCyrillic?ru:binding.Original;
   // OnEnable must never pump global ResourceManager operations or scene loads.
   reference.WaitForCompletion=false;
   if(reference.LocaleOverride!=desired)reference.LocaleOverride=desired;
   if(useCyrillic){
    string key=FontKey(reference);
    ReadyFonts.TryGetValue(key,out var font);
    if(font==null)QueueFont(reference);
    var text=component.GetComponent<TMP_Text>();
    if(font!=null&&text!=null&&text.font!=font)component.OnUpdateAsset.Invoke(font);
   }
   var operation=reference.CurrentLoadingOperationHandle;
   if(useCyrillic&&operation.IsValid()&&operation.IsDone&&operation.Result!=null&&binding.LastFont!=operation.Result.name){
    binding.LastFont=operation.Result.name;
    LocalizationPlugin.Self.Log.LogInfo("Original font mapping: "+component.gameObject.name+" -> "+binding.LastFont);
   }
 }
 internal static void Refresh(){
  bool enabled=LocalizationPlugin.CanChangeTextures();
  for(int i=Rows.Count-1;i>=0;i--){var c=Rows[i].Item1;var row=Rows[i].Item2;
   if(c==null||row==null){Rows.RemoveAt(i);continue;}
   c.interactable=enabled;int v=LocalizationPlugin.TextureChoice.Value?1:0;if(c.m_value!=v){c.m_value=v;c.UpdateDisplay();}
   if(c.m_labelLocalizeString!=null){c.m_labelLocalizeString.enabled=false;var t=c.m_labelLocalizeString.GetComponent<TMP_Text>();if(t!=null)t.text="Текстури";}
  }
 }
}
