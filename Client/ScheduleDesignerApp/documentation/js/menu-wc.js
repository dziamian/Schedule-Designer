'use strict';

customElements.define('compodoc-menu', class extends HTMLElement {
    constructor() {
        super();
        this.isNormalMode = this.getAttribute('mode') === 'normal';
    }

    connectedCallback() {
        this.render(this.isNormalMode);
    }

    render(isNormalMode) {
        let tp = lithtml.html(`
        <nav>
            <ul class="list">
                <li class="title">
                    <a href="index.html" data-type="index-link">schedule-designer-app documentation</a>
                </li>

                <li class="divider"></li>
                ${ isNormalMode ? `<div id="book-search-input" role="search"><input type="text" placeholder="Type to search"></div>` : '' }
                <li class="chapter">
                    <a data-type="chapter-link" href="index.html"><span class="icon ion-ios-home"></span>Getting started</a>
                    <ul class="links">
                        <li class="link">
                            <a href="overview.html" data-type="chapter-link">
                                <span class="icon ion-ios-keypad"></span>Overview
                            </a>
                        </li>
                        <li class="link">
                            <a href="index.html" data-type="chapter-link">
                                <span class="icon ion-ios-paper"></span>README
                            </a>
                        </li>
                                <li class="link">
                                    <a href="dependencies.html" data-type="chapter-link">
                                        <span class="icon ion-ios-list"></span>Dependencies
                                    </a>
                                </li>
                    </ul>
                </li>
                    <li class="chapter modules">
                        <a data-type="chapter-link" href="modules.html">
                            <div class="menu-toggler linked" data-toggle="collapse" ${ isNormalMode ?
                                'data-target="#modules-links"' : 'data-target="#xs-modules-links"' }>
                                <span class="icon ion-ios-archive"></span>
                                <span class="link-name">Modules</span>
                                <span class="icon ion-ios-arrow-down"></span>
                            </div>
                        </a>
                        <ul class="links collapse " ${ isNormalMode ? 'id="modules-links"' : 'id="xs-modules-links"' }>
                            <li class="link">
                                <a href="modules/AppModule.html" data-type="entity-link" >AppModule</a>
                                    <li class="chapter inner">
                                        <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ?
                                            'data-target="#components-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' : 'data-target="#xs-components-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' }>
                                            <span class="icon ion-md-cog"></span>
                                            <span>Components</span>
                                            <span class="icon ion-ios-arrow-down"></span>
                                        </div>
                                        <ul class="links collapse" ${ isNormalMode ? 'id="components-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' :
                                            'id="xs-components-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' }>
                                            <li class="link">
                                                <a href="components/AddRoomSelectionComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >AddRoomSelectionComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/AdminResourcesComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >AdminResourcesComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/AdministratorPanelComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >AdministratorPanelComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/AppComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >AppComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/AuthenticatedComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >AuthenticatedComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/AvailableResourcesComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >AvailableResourcesComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/ClearFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ClearFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/CourseComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >CourseComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/CourseEditionFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >CourseEditionFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/CourseFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >CourseFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/CourseTypeFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >CourseTypeFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/ExportFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ExportFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/FullScheduleComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >FullScheduleComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/GroupFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >GroupFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/ImportFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ImportFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/MyCoursesComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >MyCoursesComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/PersonalScheduleComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >PersonalScheduleComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/ProfileComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ProfileComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/RoomFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >RoomFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/RoomSelectionComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >RoomSelectionComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/RoomTypeFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >RoomTypeFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/ScheduleComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ScheduleComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/ScheduledChangesViewComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ScheduledChangesViewComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/SelectViewComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >SelectViewComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/SettingsFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >SettingsFieldComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/StudentScheduleComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >StudentScheduleComponent</a>
                                            </li>
                                            <li class="link">
                                                <a href="components/UserFieldComponent.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >UserFieldComponent</a>
                                            </li>
                                        </ul>
                                    </li>
                                <li class="chapter inner">
                                    <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ?
                                        'data-target="#injectables-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' : 'data-target="#xs-injectables-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' }>
                                        <span class="icon ion-md-arrow-round-down"></span>
                                        <span>Injectables</span>
                                        <span class="icon ion-ios-arrow-down"></span>
                                    </div>
                                    <ul class="links collapse" ${ isNormalMode ? 'id="injectables-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' :
                                        'id="xs-injectables-links-module-AppModule-7a7f1a34d0e0ff99defd18a3223382a497daccd714fc9ba349bfe11607cb7b20f2e4f4d09fa6733c29c741452e563915e3d29b908c0a50fd6f7bdc0115624acd"' }>
                                        <li class="link">
                                            <a href="injectables/ScheduleDesignerApiService.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >ScheduleDesignerApiService</a>
                                        </li>
                                        <li class="link">
                                            <a href="injectables/SignalrService.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >SignalrService</a>
                                        </li>
                                        <li class="link">
                                            <a href="injectables/UsosApiService.html" data-type="entity-link" data-context="sub-entity" data-context-id="modules" >UsosApiService</a>
                                        </li>
                                    </ul>
                                </li>
                            </li>
                            <li class="link">
                                <a href="modules/AppRoutingModule.html" data-type="entity-link" >AppRoutingModule</a>
                            </li>
                </ul>
                </li>
                    <li class="chapter">
                        <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ? 'data-target="#components-links"' :
                            'data-target="#xs-components-links"' }>
                            <span class="icon ion-md-cog"></span>
                            <span>Components</span>
                            <span class="icon ion-ios-arrow-down"></span>
                        </div>
                        <ul class="links collapse " ${ isNormalMode ? 'id="components-links"' : 'id="xs-components-links"' }>
                            <li class="link">
                                <a href="components/LoginComponent.html" data-type="entity-link" >LoginComponent</a>
                            </li>
                            <li class="link">
                                <a href="components/LoginComponent-1.html" data-type="entity-link" >LoginComponent</a>
                            </li>
                        </ul>
                    </li>
                    <li class="chapter">
                        <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ? 'data-target="#classes-links"' :
                            'data-target="#xs-classes-links"' }>
                            <span class="icon ion-ios-paper"></span>
                            <span>Classes</span>
                            <span class="icon ion-ios-arrow-down"></span>
                        </div>
                        <ul class="links collapse " ${ isNormalMode ? 'id="classes-links"' : 'id="xs-classes-links"' }>
                            <li class="link">
                                <a href="classes/AccessToken.html" data-type="entity-link" >AccessToken</a>
                            </li>
                            <li class="link">
                                <a href="classes/AddedSchedulePositions.html" data-type="entity-link" >AddedSchedulePositions</a>
                            </li>
                            <li class="link">
                                <a href="classes/AddRoomSelectionDialogData.html" data-type="entity-link" >AddRoomSelectionDialogData</a>
                            </li>
                            <li class="link">
                                <a href="classes/AddRoomSelectionDialogResult.html" data-type="entity-link" >AddRoomSelectionDialogResult</a>
                            </li>
                            <li class="link">
                                <a href="classes/Coordinator.html" data-type="entity-link" >Coordinator</a>
                            </li>
                            <li class="link">
                                <a href="classes/CoordinatorBasic.html" data-type="entity-link" >CoordinatorBasic</a>
                            </li>
                            <li class="link">
                                <a href="classes/Course.html" data-type="entity-link" >Course</a>
                            </li>
                            <li class="link">
                                <a href="classes/CourseDurationErrorMatcher.html" data-type="entity-link" >CourseDurationErrorMatcher</a>
                            </li>
                            <li class="link">
                                <a href="classes/CourseEdition.html" data-type="entity-link" >CourseEdition</a>
                            </li>
                            <li class="link">
                                <a href="classes/CourseEditionInfo.html" data-type="entity-link" >CourseEditionInfo</a>
                            </li>
                            <li class="link">
                                <a href="classes/CourseInfo.html" data-type="entity-link" >CourseInfo</a>
                            </li>
                            <li class="link">
                                <a href="classes/CourseType.html" data-type="entity-link" >CourseType</a>
                            </li>
                            <li class="link">
                                <a href="classes/Filter.html" data-type="entity-link" >Filter</a>
                            </li>
                            <li class="link">
                                <a href="classes/Group.html" data-type="entity-link" >Group</a>
                            </li>
                            <li class="link">
                                <a href="classes/GroupInfo.html" data-type="entity-link" >GroupInfo</a>
                            </li>
                            <li class="link">
                                <a href="classes/MessageObject.html" data-type="entity-link" >MessageObject</a>
                            </li>
                            <li class="link">
                                <a href="classes/ModifiedSchedulePositions.html" data-type="entity-link" >ModifiedSchedulePositions</a>
                            </li>
                            <li class="link">
                                <a href="classes/ModifyingScheduleData.html" data-type="entity-link" >ModifyingScheduleData</a>
                            </li>
                            <li class="link">
                                <a href="classes/PeriodsErrorMatcher.html" data-type="entity-link" >PeriodsErrorMatcher</a>
                            </li>
                            <li class="link">
                                <a href="classes/RemovedSchedulePositions.html" data-type="entity-link" >RemovedSchedulePositions</a>
                            </li>
                            <li class="link">
                                <a href="classes/ResourceFlatNode.html" data-type="entity-link" >ResourceFlatNode</a>
                            </li>
                            <li class="link">
                                <a href="classes/ResourceItem.html" data-type="entity-link" >ResourceItem</a>
                            </li>
                            <li class="link">
                                <a href="classes/ResourceNode.html" data-type="entity-link" >ResourceNode</a>
                            </li>
                            <li class="link">
                                <a href="classes/Room.html" data-type="entity-link" >Room</a>
                            </li>
                            <li class="link">
                                <a href="classes/RoomSelect.html" data-type="entity-link" >RoomSelect</a>
                            </li>
                            <li class="link">
                                <a href="classes/RoomSelectionDialogData.html" data-type="entity-link" >RoomSelectionDialogData</a>
                            </li>
                            <li class="link">
                                <a href="classes/RoomSelectionDialogResult.html" data-type="entity-link" >RoomSelectionDialogResult</a>
                            </li>
                            <li class="link">
                                <a href="classes/RoomType.html" data-type="entity-link" >RoomType</a>
                            </li>
                            <li class="link">
                                <a href="classes/ScheduledChangesDialogData.html" data-type="entity-link" >ScheduledChangesDialogData</a>
                            </li>
                            <li class="link">
                                <a href="classes/ScheduledChangesDialogResult.html" data-type="entity-link" >ScheduledChangesDialogResult</a>
                            </li>
                            <li class="link">
                                <a href="classes/ScheduledMove.html" data-type="entity-link" >ScheduledMove</a>
                            </li>
                            <li class="link">
                                <a href="classes/ScheduledMoveDetails.html" data-type="entity-link" >ScheduledMoveDetails</a>
                            </li>
                            <li class="link">
                                <a href="classes/ScheduledMoveInfo.html" data-type="entity-link" >ScheduledMoveInfo</a>
                            </li>
                            <li class="link">
                                <a href="classes/SchedulePosition.html" data-type="entity-link" >SchedulePosition</a>
                            </li>
                            <li class="link">
                                <a href="classes/ScheduleSlot.html" data-type="entity-link" >ScheduleSlot</a>
                            </li>
                            <li class="link">
                                <a href="classes/SearchUser.html" data-type="entity-link" >SearchUser</a>
                            </li>
                            <li class="link">
                                <a href="classes/SelectedCourseEdition.html" data-type="entity-link" >SelectedCourseEdition</a>
                            </li>
                            <li class="link">
                                <a href="classes/SelectViewDialogData.html" data-type="entity-link" >SelectViewDialogData</a>
                            </li>
                            <li class="link">
                                <a href="classes/SelectViewDialogResult.html" data-type="entity-link" >SelectViewDialogResult</a>
                            </li>
                            <li class="link">
                                <a href="classes/Settings.html" data-type="entity-link" >Settings</a>
                            </li>
                            <li class="link">
                                <a href="classes/Staff.html" data-type="entity-link" >Staff</a>
                            </li>
                            <li class="link">
                                <a href="classes/Student.html" data-type="entity-link" >Student</a>
                            </li>
                            <li class="link">
                                <a href="classes/StudentBasic.html" data-type="entity-link" >StudentBasic</a>
                            </li>
                            <li class="link">
                                <a href="classes/Titles.html" data-type="entity-link" >Titles</a>
                            </li>
                            <li class="link">
                                <a href="classes/UnitsMinutesErrorMatcher.html" data-type="entity-link" >UnitsMinutesErrorMatcher</a>
                            </li>
                            <li class="link">
                                <a href="classes/User.html" data-type="entity-link" >User</a>
                            </li>
                            <li class="link">
                                <a href="classes/UserBasic.html" data-type="entity-link" >UserBasic</a>
                            </li>
                            <li class="link">
                                <a href="classes/UserInfo.html" data-type="entity-link" >UserInfo</a>
                            </li>
                        </ul>
                    </li>
                        <li class="chapter">
                            <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ? 'data-target="#injectables-links"' :
                                'data-target="#xs-injectables-links"' }>
                                <span class="icon ion-md-arrow-round-down"></span>
                                <span>Injectables</span>
                                <span class="icon ion-ios-arrow-down"></span>
                            </div>
                            <ul class="links collapse " ${ isNormalMode ? 'id="injectables-links"' : 'id="xs-injectables-links"' }>
                                <li class="link">
                                    <a href="injectables/AdministratorApiService.html" data-type="entity-link" >AdministratorApiService</a>
                                </li>
                                <li class="link">
                                    <a href="injectables/ResourceTreeService.html" data-type="entity-link" >ResourceTreeService</a>
                                </li>
                                <li class="link">
                                    <a href="injectables/ScheduleDesignerApiService.html" data-type="entity-link" >ScheduleDesignerApiService</a>
                                </li>
                                <li class="link">
                                    <a href="injectables/ScheduleInteractionService.html" data-type="entity-link" >ScheduleInteractionService</a>
                                </li>
                                <li class="link">
                                    <a href="injectables/SignalrService.html" data-type="entity-link" >SignalrService</a>
                                </li>
                                <li class="link">
                                    <a href="injectables/UsosApiService.html" data-type="entity-link" >UsosApiService</a>
                                </li>
                            </ul>
                        </li>
                    <li class="chapter">
                        <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ? 'data-target="#guards-links"' :
                            'data-target="#xs-guards-links"' }>
                            <span class="icon ion-ios-lock"></span>
                            <span>Guards</span>
                            <span class="icon ion-ios-arrow-down"></span>
                        </div>
                        <ul class="links collapse " ${ isNormalMode ? 'id="guards-links"' : 'id="xs-guards-links"' }>
                            <li class="link">
                                <a href="guards/AuthGuardService.html" data-type="entity-link" >AuthGuardService</a>
                            </li>
                            <li class="link">
                                <a href="guards/NoAuthGuardService.html" data-type="entity-link" >NoAuthGuardService</a>
                            </li>
                        </ul>
                    </li>
                    <li class="chapter">
                        <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ? 'data-target="#interfaces-links"' :
                            'data-target="#xs-interfaces-links"' }>
                            <span class="icon ion-md-information-circle-outline"></span>
                            <span>Interfaces</span>
                            <span class="icon ion-ios-arrow-down"></span>
                        </div>
                        <ul class="links collapse " ${ isNormalMode ? ' id="interfaces-links"' : 'id="xs-interfaces-links"' }>
                            <li class="link">
                                <a href="interfaces/ICourse.html" data-type="entity-link" >ICourse</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/ICourseEdition.html" data-type="entity-link" >ICourseEdition</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/ICourseType.html" data-type="entity-link" >ICourseType</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/IGroup.html" data-type="entity-link" >IGroup</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/IRoom.html" data-type="entity-link" >IRoom</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/IRoomType.html" data-type="entity-link" >IRoomType</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/ISettings.html" data-type="entity-link" >ISettings</a>
                            </li>
                            <li class="link">
                                <a href="interfaces/IUserInfo.html" data-type="entity-link" >IUserInfo</a>
                            </li>
                        </ul>
                    </li>
                    <li class="chapter">
                        <div class="simple menu-toggler" data-toggle="collapse" ${ isNormalMode ? 'data-target="#miscellaneous-links"'
                            : 'data-target="#xs-miscellaneous-links"' }>
                            <span class="icon ion-ios-cube"></span>
                            <span>Miscellaneous</span>
                            <span class="icon ion-ios-arrow-down"></span>
                        </div>
                        <ul class="links collapse " ${ isNormalMode ? 'id="miscellaneous-links"' : 'id="xs-miscellaneous-links"' }>
                            <li class="link">
                                <a href="miscellaneous/enumerations.html" data-type="entity-link">Enums</a>
                            </li>
                            <li class="link">
                                <a href="miscellaneous/functions.html" data-type="entity-link">Functions</a>
                            </li>
                            <li class="link">
                                <a href="miscellaneous/variables.html" data-type="entity-link">Variables</a>
                            </li>
                        </ul>
                    </li>
                        <li class="chapter">
                            <a data-type="chapter-link" href="routes.html"><span class="icon ion-ios-git-branch"></span>Routes</a>
                        </li>
                    <li class="chapter">
                        <a data-type="chapter-link" href="coverage.html"><span class="icon ion-ios-stats"></span>Documentation coverage</a>
                    </li>
                    <li class="divider"></li>
                    <li class="copyright">
                        Documentation generated using <a href="https://compodoc.app/" target="_blank">
                            <img data-src="images/compodoc-vectorise.png" class="img-responsive" data-type="compodoc-logo">
                        </a>
                    </li>
            </ul>
        </nav>
        `);
        this.innerHTML = tp.strings;
    }
});