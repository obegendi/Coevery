﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Coevery.ContentManagement;
using Coevery.ContentManagement.Drivers;
using Coevery.ContentManagement.MetaData;
using Coevery.ContentManagement.Records;
using Coevery.Data;
using Coevery.Relationship.Models;
using Coevery.Relationship.Services;
using Coevery.Relationship.ViewModels;
using JetBrains.Annotations;

namespace Coevery.Relationship.Drivers {
    [UsedImplicitly]
    public abstract class DynamicRelatedPartDriver<TRelatedPart, TContentLinkRecord>
        : ContentPartDriver<TRelatedPart>
        where TRelatedPart : ContentPart, new()
        where TContentLinkRecord : ContentLinkRecord, new() {
        private readonly IDynamicRelatedService<TContentLinkRecord> _relatedService;
        private readonly IContentManager _contentManager;
        private readonly IRepository<TContentLinkRecord> _contentLinkRepository;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        protected string _entityName;

        private const string TemplateName = "Parts/Relationship.Edit";

        protected DynamicRelatedPartDriver(
            IDynamicRelatedService<TContentLinkRecord> relatedService,
            IContentManager contentManager,
            IRepository<TContentLinkRecord> contentLinkRepository,
            IContentDefinitionManager contentDefinitionManager) {
            _relatedService = relatedService;
            _contentManager = contentManager;
            _contentLinkRepository = contentLinkRepository;
            _contentDefinitionManager = contentDefinitionManager;
        }

        private static string GetPrefix(ContentPart part) {
            return part.PartDefinition.Name;
        }

        protected override DriverResult Editor(TRelatedPart part, dynamic shapeHelper) {
            return ContentShape("Parts_Relationship_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: TemplateName,
                    Model: BuildEditorViewModel(part),
                    Prefix: GetPrefix(part)));
        }

        protected override DriverResult Editor(TRelatedPart part, IUpdateModel updater, dynamic shapeHelper) {
            var model = new EditRelationshipViewModel();
            updater.TryUpdateModel(model, GetPrefix(part), null, null);

            if (part.ContentItem.Id != 0) {
                _relatedService.UpdateForContentItem(part.ContentItem, model.SelectedIds);
            }

            return Editor(part, shapeHelper);
        }

        private IEnumerable<ContentItemRecord> GetLinks(TRelatedPart part) {
            return _contentLinkRepository.Table
                .Where(x => x.RelatedPartRecord.Id == part.Id)
                .Select(x => x.PrimaryPartRecord);
        }

        private EditRelationshipViewModel BuildEditorViewModel(TRelatedPart part) {
            return new EditRelationshipViewModel {
                Links = _relatedService.GetLinks(_entityName).Select(r => new SelectListItem() {
                    Value = r.Id.ToString(),
                    Text = _contentManager.GetItemMetadata(r).DisplayText,
                }).ToList(),
                SelectedIds = GetLinks(part).Select(x => x.Id.ToString()).ToArray(),
                DisplayName = _contentDefinitionManager.GetPartDefinition(typeof (TRelatedPart).Name).Settings["DisplayName"]
            };
        }
    }
}