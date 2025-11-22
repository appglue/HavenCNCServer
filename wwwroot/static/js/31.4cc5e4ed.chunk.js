"use strict";
(self["webpackChunkhavencnc"] = self["webpackChunkhavencnc"] || []).push([[31],{

/***/ 2031:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SimulatorDialog: () => (/* binding */ SimulatorDialog)
/* harmony export */ });
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(35);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(5316);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(9347);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(2518);
/* harmony import */ var _data_Job__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(42);
/* harmony import */ var _data_MachineData__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(7478);
/* harmony import */ var _data_MachineStateData__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(3361);
/* harmony import */ var _MachineViewer_FullSizeCNCViewer__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(6573);
/* harmony import */ var react_jsx_runtime__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(579);
function SimulatorDialog(_ref){let{open,onClose,simulatorViewData,maxWidth='xl'}=_ref;const machineState=_data_MachineStateData__WEBPACK_IMPORTED_MODULE_6__.MachineStateData.Instance;const machineData=_data_MachineData__WEBPACK_IMPORTED_MODULE_5__.MachineData.Instance;const job=machineState.currentJob||new _data_Job__WEBPACK_IMPORTED_MODULE_4__/* .Job */ ._();return/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_8__.jsxs)(_mui_material__WEBPACK_IMPORTED_MODULE_0__/* ["default"] */ .A,{open:open,onClose:onClose,maxWidth:maxWidth,fullWidth:true,PaperProps:{sx:{height:'90vh'}},children:[/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_8__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_1__/* ["default"] */ .A,{sx:{p:0,height:'100%'},children:/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_8__.jsx)(_MachineViewer_FullSizeCNCViewer__WEBPACK_IMPORTED_MODULE_7__/* .FullSizeCNCViewer */ .j,{simulatorViewData:simulatorViewData,machineState:machineState,machineData:machineData,job:job,enableSpindleDrag:true,showSettingsButton:true})}),/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_8__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_2__/* ["default"] */ .A,{children:/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_8__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_3__/* ["default"] */ .A,{onClick:onClose,variant:"contained",children:"Close"})})]});}

/***/ })

}]);
//# sourceMappingURL=31.4cc5e4ed.chunk.js.map