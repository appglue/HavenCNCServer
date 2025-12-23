"use strict";
(self["webpackChunkhavencnc"] = self["webpackChunkhavencnc"] || []).push([[31],{

/***/ 2031:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SimulatorDialog: () => (/* binding */ SimulatorDialog)
/* harmony export */ });
/* harmony import */ var _mui_icons_material__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(3438);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(35);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(6600);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(7392);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(5316);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(9347);
/* harmony import */ var _mui_material__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(2518);
/* harmony import */ var _data_ConfigurationData__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(7677);
/* harmony import */ var _data_Job__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(42);
/* harmony import */ var _data_Machine_MachineData__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(2603);
/* harmony import */ var _data_MachineStateData__WEBPACK_IMPORTED_MODULE_10__ = __webpack_require__(6465);
/* harmony import */ var _MachineViewer_FullSizeCNCViewer__WEBPACK_IMPORTED_MODULE_11__ = __webpack_require__(6573);
/* harmony import */ var react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__ = __webpack_require__(579);
function SimulatorDialog(_ref){let{open,onClose,simulatorViewData,maxWidth='xl'}=_ref;const config=_data_ConfigurationData__WEBPACK_IMPORTED_MODULE_7__.ConfigurationData.Instance;const machineState=_data_MachineStateData__WEBPACK_IMPORTED_MODULE_10__.MachineStateData.Instance;const machineData=_data_Machine_MachineData__WEBPACK_IMPORTED_MODULE_9__.MachineData.Instance;const job=machineState.currentJob||new _data_Job__WEBPACK_IMPORTED_MODULE_8__/* .Job */ ._();return/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsxs)(_mui_material__WEBPACK_IMPORTED_MODULE_1__/* ["default"] */ .A,{open:open,onClose:onClose,maxWidth:maxWidth,fullWidth:true,PaperProps:{sx:{height:'90vh'}},children:[/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsxs)(_mui_material__WEBPACK_IMPORTED_MODULE_2__/* ["default"] */ .A,{sx:{display:'flex',alignItems:'center',justifyContent:'space-between',backgroundColor:config.primaryColor,color:'white',py:2,px:3},children:["Job Simulator",/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_3__/* ["default"] */ .A,{onClick:onClose,sx:{color:'white','&:hover':{backgroundColor:'rgba(255, 255, 255, 0.1)'}},children:/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsx)(_mui_icons_material__WEBPACK_IMPORTED_MODULE_0__/* ["default"] */ .A,{})})]}),/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_4__/* ["default"] */ .A,{sx:{p:0,height:'100%'},children:/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsx)(_MachineViewer_FullSizeCNCViewer__WEBPACK_IMPORTED_MODULE_11__/* .FullSizeCNCViewer */ .j,{simulatorViewData:simulatorViewData,machineState:machineState,machineData:machineData,job:job,enableSpindleDrag:true,showSettingsButton:true})}),/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_5__/* ["default"] */ .A,{sx:{p:2,justifyContent:'flex-end',borderTop:`1px solid ${config.borderColor}`},children:/*#__PURE__*/(0,react_jsx_runtime__WEBPACK_IMPORTED_MODULE_12__.jsx)(_mui_material__WEBPACK_IMPORTED_MODULE_6__/* ["default"] */ .A,{onClick:onClose,variant:"contained",sx:{backgroundColor:config.primaryColor,'&:hover':{backgroundColor:config.primaryColor,opacity:0.9}},children:"Close"})})]});}

/***/ })

}]);
//# sourceMappingURL=31.4ecb497c.chunk.js.map