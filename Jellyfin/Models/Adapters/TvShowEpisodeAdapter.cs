﻿using Jellyfin.Core;
using Jellyfin.Models.ServiceModels;

namespace Jellyfin.Models.Adapters
{
    /// <inheritdoc />
    /// <summary>
    /// Adapter to map the JSON tv show episode to episode model.
    /// </summary>
    public class TvShowEpisodeAdapter : ItemMediaElementBaseAdapter, IAdapter<TvShowEpisodeItem, TvShowEpisode>
    {
        #region Additional methods

        public TvShowEpisode Convert(TvShowEpisodeItem source)
        {
            TvShowEpisode t = new TvShowEpisode();
            ConvertBase(source, t);

            t.SeriesName = source.SeriesName;
            t.SeriesId = source.SeriesId;
            t.SeasonId = source.SeasonId;
            t.SeasonName = source.SeasonName;
            t.PremiereDate = source.PremiereDate;
            t.IndexNumber = source.IndexNumber;
            t.Description = source.Overview;

            if (source.ImageTags != null) { 
                t.BackdropImageId = source.ImageTags.Primary;
            }

            return t;
        }

        #endregion
    }
}