﻿using Newtonsoft.Json;
using PalCalc.Model;
using PalCalc.SaveReader;
using PalCalc.SaveReader.SaveFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.Model
{
    public class CachedSaveGame
    {
        public DateTime LastModified { get; set; }
        public string FolderPath { get; set; }

        public bool IsServerSave { get; set; }

        public string WorldName { get; set; }
        public string PlayerName { get; set; }
        public int? PlayerLevel { get; set; }
        public int InGameDay { get; set; }

        public List<PlayerInstance> Players { get; set; }
        public List<GuildInstance> Guilds { get; set; }
        public List<PalInstance> OwnedPals { get; set; }

        private Dictionary<string, PlayerInstance> playersByName;
        public Dictionary<string, PlayerInstance> PlayersById =>
            playersByName ??= Players.ToDictionary(p => p.PlayerId);

        public Dictionary<string, PlayerInstance> PlayersByInstanceId =>
            playersByName ??= Players.ToDictionary(p => p.InstanceId);

        private Dictionary<string, GuildInstance> playerGuilds;
        public Dictionary<string, GuildInstance> GuildsByPlayerId =>
            playerGuilds ??= Players.ToDictionary(p => p.PlayerId, p => Guilds.FirstOrDefault(g => g.MemberIds.Contains(p.PlayerId)));

        public SaveGame UnderlyingSave => new SaveGame(FolderPath);

        public bool IsValid => UnderlyingSave.IsValid;

        public bool IsOutdated => LastModified != UnderlyingSave.LastModified;

        public string StateId => $"{IdentifierFor(UnderlyingSave)}-{LastModified.Ticks}";

        public static event Action<SaveGame> SaveFileLoadStart;
        public static event Action<SaveGame> SaveFileLoadEnd;
        public static event Action<SaveGame, Exception> SaveFileLoadError;

        public static string IdentifierFor(SaveGame game)
        {
            var userFolderName = Path.GetFileName(Path.GetDirectoryName(game.BasePath));
            var saveName = game.FolderName;

            return $"{userFolderName}-{saveName}";
        }

        public static CachedSaveGame FromSaveGame(SaveGame game, PalDB db)
        {
            SaveFileLoadStart?.Invoke(game);

            CachedSaveGame result;
#if HANDLE_ERRORS
            try
            {
#endif
                var meta = game.LevelMeta.ReadGameOptions();
                var charData = game.Level.ReadCharacterData(db);
                result = new CachedSaveGame()
                {
                    LastModified = game.LastModified,
                    FolderPath = game.BasePath,
                    OwnedPals = charData.Pals,
                    Guilds = charData.Guilds,
                    Players = charData.Players,
                    PlayerLevel = meta.PlayerLevel,
                    PlayerName = meta.PlayerName,
                    WorldName = meta.WorldName,
                    InGameDay = meta.InGameDay,
                };
#if HANDLE_ERRORS
            }
            catch (Exception ex)
            {
                SaveFileLoadError?.Invoke(game, ex);
                return null;
            }
#endif

            SaveFileLoadEnd?.Invoke(game);

            return result;
        }

        public string ToJson(PalDB db) => JsonConvert.SerializeObject(this, new PalInstanceJsonConverter(db));

        public static CachedSaveGame FromJson(string json, PalDB db) => JsonConvert.DeserializeObject<CachedSaveGame>(json, new PalInstanceJsonConverter(db));

    }
}
