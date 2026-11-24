using System;
using System.Collections.Generic;

namespace JustOneMorePeel
{
    internal sealed class PeelNoticeCollection
    {
        internal const int SeriesCount = 2;

        // The paired print textures share this asset name in both languages.
        static readonly (string id, int series, string ru, string en, string noteRu, string noteEn)[] Catalogue =
        {
            ("01_LostCat", 0, "Пропал кот", "Missing cat", "Рыжего кота зовут Батон. На листе ещё остались отрывные полоски.", "The ginger cat is called Baton. Some of the tear-off tabs are still attached."),
            ("06_Neighbours", 0, "Соседи!", "Neighbours!", "Записка из квартиры 8. Кота просят не запирать в подвале.", "A note from flat 8 asks neighbours to keep the cat out of the locked basement."),
            ("07_OldWarning", 0, "Объявления не клеить", "Do not post notices", "Предупреждение дворника на свежей краске. На листе стоит 1987 год.", "The caretaker warned people about the freshly painted wall in 1987."),
            ("08_Electrician", 0, "Электрик по соседству", "Neighbourhood electrician", "Павел из квартиры 12 просит звонить в дверь после шести.", "Pavel from flat 12 asks callers to ring his doorbell after six."),
            ("12_BookExchange", 0, "Помощник в магазин", "Shop assistant wanted", "Магазин ищет помощника для приёмки товара и работы в зале.", "The shop needs help receiving deliveries and working on the shop floor."),
            ("14_FoundKeys", 0, "Найдены ключи", "Keys found", "Связку нашли вечером у арки. Ключи оставили в первом подъезде.", "The keys were found by the arch in the evening and left at entrance 1."),
            ("15_WaterNotice", 0, "Отключение воды", "Water shutoff", "Управляющая организация предупредила два подъезда о ремонте трубы.", "Building management warned two entrances about pipe repairs."),
            ("17_SeedExchange", 0, "Розыск", "Wanted notice", "В ориентировке указан выход на площадь, где видели подозреваемого.", "The notice names the exit to the square as the last place the suspect was seen."),
            ("20_BreadWindow", 0, "Продавец в пекарню", "Bakery assistant wanted", "В объявлении о работе предлагают утренние смены у остановки.", "The bakery by the stop offers morning shifts."),
            ("27_CleanUp", 0, "Субботник", "Courtyard clean-up", "В плане уборки есть перерыв на чай. Перчатки выдадут во дворе.", "The clean-up schedule includes a tea break. Gloves are provided in the courtyard."),
            ("29_ParcelNote", 0, "Посылка для квартиры 12", "Parcel for flat 12", "Нина из квартиры 6 забрала посылку и просит зайти вечером.", "Nina from flat 6 picked up the parcel and asks the neighbour to stop by that evening."),
            ("32_ArchiveLoan", 0, "Лист выдачи", "Archive loan slip", "Папку заявки №17 нужно вернуть на полку Б.", "The folder for work order 17 belongs back on shelf B."),
            ("03_QuietYard", 1, "Тише двора", "Quieter than the courtyard", "На афише соседского концерта указаны пианино и бас.", "Piano and bass are listed on the flyer for a neighbourhood concert."),
            ("10_OldCinema", 1, "Ночь короткого метра", "Short film night", "Киноклуб обещает четыре фильма и один проектор.", "The film club promises four shorts and one projector."),
            ("16_ChessClub", 1, "Шахматы по четвергам", "Chess on Thursdays", "Игроков любого уровня ждали у мастерской в половине седьмого.", "Players of any level were welcome by the workshop at half past six."),
            ("22_PhotoWalk", 1, "Обмен кассет", "Tape swap", "На встречу у арки приглашали принести свои записи.", "Neighbours were invited to bring their own tapes to the arch."),
            ("23_YardTheatre", 1, "Шум города", "City noise", "Три группы выступали в клубе у трамвайного кольца.", "Three bands played at the club by the tram loop."),
            ("25_PianoLessons", 1, "Уроки фортепиано", "Piano lessons", "Мария Ивановна приглашала на уроки детей и взрослых.", "Maria Ivanovna offered lessons for children and adults."),
            ("30_OldSummerFair", 1, "Вечер пластинок", "Record night", "Дом культуры приглашал потанцевать под музыку из домашней коллекции.", "The community centre invited neighbours to dance to records from a home collection.")
        };

        internal sealed class Card
        {
            internal readonly string Id;
            internal readonly int Design;
            internal readonly int Series;
            readonly string titleRu, titleEn, noteRu, noteEn;
            internal int AvailableBays, FoundBays;

            internal Card(int design, (string id, int series, string ru, string en, string noteRu, string noteEn) entry)
            {
                Design = design;
                Id = entry.id;
                Series = entry.series;
                titleRu = entry.ru;
                titleEn = entry.en;
                noteRu = entry.noteRu;
                noteEn = entry.noteEn;
            }

            internal string Title(bool ru) => ru ? titleRu : titleEn;
            internal string Note(bool ru) => ru ? noteRu : noteEn;
        }

        readonly Card[] cardsByDesign;
        readonly List<Card> cards = new();

        internal PeelNoticeCollection(PeelSettings settings, IReadOnlyList<PeelPosterStack> stacks)
        {
            cardsByDesign = new Card[settings.PaperCount];
            var keys = new string[cardsByDesign.Length];
            for (int i = 0; i < keys.Length; i++) keys[i] = settings.PaperVariantId(i);

            foreach (var entry in Catalogue)
            {
                Card card = null;
                for (int design = 0; design < keys.Length; design++)
                {
                    if (keys[design] != entry.id) continue;
                    card ??= new Card(design, entry);
                    cardsByDesign[design] = card;
                }
                if (card != null) cards.Add(card);
            }

            foreach (var stack in stacks)
            {
                int bay = BayMask(stack.PersistentId);
                for (int layer = 0; layer < stack.PosterCount; layer++)
                {
                    var card = cardsByDesign[stack.DesignAt(layer)];
                    if (card != null) card.AvailableBays |= bay;
                }
            }
        }

        internal Card RecordClearedSheet(PeelPosterStack stack, int layer)
        {
            var card = cardsByDesign[stack.DesignAt(layer)];
            int bay = BayMask(stack.PersistentId);
            if (card == null || bay == 0) return null;
            bool first = card.FoundBays == 0;
            card.FoundBays |= bay;
            return first ? card : null;
        }

        internal void Restore(IReadOnlyList<PeelPosterStack> stacks)
        {
            foreach (var card in cards) card.FoundBays = 0;
            foreach (var stack in stacks)
                for (int layer = 0; layer < stack.ClearedPosters; layer++)
                    RecordClearedSheet(stack, layer);
        }

        internal (int found, int total, int searchBays) SeriesProgress(int series)
        {
            int found = 0, total = 0, searchBays = 0;
            foreach (var card in cards)
            {
                if (card.Series != series || card.AvailableBays == 0) continue;
                total++;
                if (card.FoundBays != 0) found++;
                else searchBays |= card.AvailableBays;
            }
            return (found, total, searchBays);
        }

        internal IReadOnlyList<Card> Cards => cards;

        static int BayMask(string id)
        {
            const string prefix = "pavilion.bay";
            if (id == null || id.Length <= prefix.Length || !id.StartsWith(prefix, StringComparison.Ordinal)) return 0;
            int bay = id[prefix.Length] - '1';
            return bay >= 0 && bay < 4 ? 1 << bay : 0;
        }
    }
}
