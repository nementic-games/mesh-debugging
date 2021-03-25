# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2021-03-25
### Added
- Support for selecting multiple GameObjets to inspect their meshes.
- The 'Include Children' toolbar option toggles between only searching the top-level selection and all children.
- The 'Hide Textures' toolbar option sets all inspected target texture previews to alpha zero.
### Fixed
- Improved robustness against GameObjects with missing components (MeshFilter, MeshRenderer, SkinnedMeshRenderer) and
missing mesh or material references.

## [1.1.0] - 2020-3-01
### Added
- Three modes for the mesh source: from selection, explicit mesh asset or from GameObject.
- Options sidebar panel.
- Texture preview.
### Changed
- Renamed 'Recenter' to 'Focus'. Now, this action focuses on the bounds of the UV map instead of the graph origin.

## [1.0.0] - 2019-1-13
### Added
- Initial release of the tool.
