define([
    "dojo/_base/declare",
    "dojo/topic",
    "dojo/on",
    "epi-cms/contentediting/OnPageEditing"
], function (
    declare,
    topic,
    on,
    OnPageEditing
) {
    return declare([OnPageEditing], {
        _setViewModelAttr: function () {
            this.inherited(arguments);
            this.ownByKey("updateChangedProperty", topic.subscribe("updateChangedProperty", this._updateChangedProperty.bind(this)));
        },

        _updateChangedProperty: function (propertyName) {
            this.viewModel.beginOperation();
            this.viewModel.setProperty("iversionable_changed", new Date(), null);

            var handler = on(this.viewModel, "saved", function (result) {
                handler.remove();

                if(!result) {
                    return;
                }
                var mapping = this._mappingManager.findOne("propertyName", propertyName);
                mapping.updateController.contentLink = result.contentLink;
                mapping.updateController.render(true);
            }.bind(this));
        }
    });
});
