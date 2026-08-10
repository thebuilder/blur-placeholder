export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "propertyEditorUi",
    alias: "TheBuilder.PropertyEditorUi.BlurPlaceholder",
    name: "Blur Placeholder property editor",
    element: () => import("./blur-placeholder.element.js"),
    meta: {
      label: "Blur placeholder",
      propertyEditorSchemaAlias: "Umbraco.Plain.String",
      icon: "icon-picture",
      group: "media",
      supportsReadOnly: true,
    },
  },
];
