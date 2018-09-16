import { Http } from '@angular/http';
import 'rxjs/add/operator/toPromise';
import { AsynchronousCollectionEditorSolver } from './AsynchronousCollectionEditorSolver';
import { BaseViewModel, CommandManagerId, IChangePropagator } from '../base.component';
import { CommandInstruction } from '../../commands/commandInstruction';
import { App, AppComponent } from '../app/app.component';

export function isComponent(obj: any): obj is BaseViewModel & { isCollection: boolean } {
    return obj != null && obj.__id !== undefined;
}
export function assert(expr: boolean, message = "Assertion failed") {
    if (!expr) {
        throw new Error(message);
    }
}
export class ChangesPropagator implements IChangePropagator {
    /** A map from component id to component. */
    private readonly components = new Map<number, BaseViewModel | any[]>();
    public readonly collectionEditor: AsynchronousCollectionEditorSolver = new AsynchronousCollectionEditorSolver(
        (component, id) => {
            // this is responsible for associating the specified id with the specified component, 
            // assuming the component is not in this.components yet
            assert(Array.from(this.components.values()).indexOf(component) === -1);
            assert(!this.components.has(id));

            // components that are not arrays have their id as property:
            if (!Array.isArray(component)) {
                component.__id = id;
            }

            this.components.set(id, component);
        });


    constructor(
        private readonly http: Http,
        private readonly baseUrl: string,
        private readonly createNewAppViewModel: () => App) {
    }

    private initializeComponents(): void {

        this.components.clear();
        const newRoot = this.createNewAppViewModel();
        this.components.set(newRoot.__id, newRoot);
        this.components.set(newRoot.commandManager.__id, newRoot.commandManager);
        console.log(`initialized view model root`);
    }

    public async open(): Promise<void> {
        this.initializeComponents();
        this.post(this.baseUrl + 'api/Changes/open');
    }
    public async registerRequest(): Promise<void> {
        this.post(this.baseUrl + 'api/Changes/RegisterRequest');
    }
    public async executeCommand(command: CommandInstruction) {
        if (command == null || command.commandName == null || command.commandName == '' || command.viewModelId < 0 || command.eventArgs == null) {
            throw new Error("Invalid command instruction");
        }
        this.post(this.baseUrl + 'api/Changes/ExecuteCommand', command);
    }

    private async post(url: string, data?: CommandInstruction) {
        try {
            console.log(`posting '${data === undefined ? '{}' : data}' to '${url.substr(this.baseUrl.length)}'. Response: `);
            const request = await this.http.post(url, data || {}).toPromise();
            const response = request.json() as IResponse;

            console.log(response);

            this.processResponse(response);
            if (response.rerequest) {
                this.registerRequest();
            }

            console.log('view model tree: ');
            console.log((<any>this.components.get(0)));
        }
        catch (error) {
            console.error(error);
        }
    }

    /**
     * If the item is a component reference, returns the associated component, otherwise returns the item itself.
     * @param value
     */
    private toInstance<T>(value: T): typeof value | BaseViewModel | any[] {
        if (isComponent(value)) {
            const result = this.components.get(value.__id);
            if (result === undefined) {
                throw new Error(`No viewmodel found for id '${value.__id}'`);
            }
            return result;
        }
        return value;
    }
    private toInstanceOrCreate<T>(value: T): typeof value | BaseViewModel | any[] {
        if (isComponent(value)) {
            const result = this.components.get(value.__id);
            if (result !== undefined) {
                return result;
            }

            if (value.isCollection)
                this.components.set(value.__id, []);
            else
                this.components.set(value.__id, value);
            return this.toInstance(value);
        }
        return value;

    }

    private processResponse(response: IResponse) {
        // assumptions:
        // the response.changes only contains properties of primitive types, that is, changes on nested components are separate entry in IResponse.changes.
        // nested component changes must be 
        for (const change of response.changes) {
            const component = this.components.get(change.id);
            if (component === undefined) {
                throw new Error(`invalid json received: component with id '${change.id}' does not exist`);
            }

            //if (ChangesPropagator.IsViewModelInstantiation(change)) {
            //    this.createNewInstance(change);
            //} else
            if (ChangesPropagator.IsPropertyChanged(change)) {
                this.processPropertyChange(<BaseViewModel>component, change);
            } else {
                const collection: any[] = change.collectionName === undefined ? component : (<any>component)[change.collectionName];
                if (ChangesPropagator.IsCollectionItemAdded(change)) {
                    const handled = this.collectionEditor.onAddedServerSideToCollection(collection, change.item, change.index, change.instructionId);
                    if (!handled) {
                        this.processItemAdded(collection, change);
                    }
                } else if (ChangesPropagator.IsCollectionItemRemoved(change)) {
                    const handled = this.collectionEditor.onRemovedServerSideFromCollection(collection, change.removedItemId, change.index, change.instructionId);
                    if (!handled) {
                        this.processItemRemoved(collection, change);
                    }
                } else if (ChangesPropagator.IsCollectionItemReordered(change)) {
                    throw new Error('reordering not implemented yet');
                } else {
                    console.log(`invalid json received: the following change could not be interpreted`);
                    console.log(change);
                }
            }
        }
    }

    private static IsViewModelInstantiation(change: IChange): change is IViewModelInstantation {
        return this.IsPropertyChanged(change) && change.propertyName == '__id';
    }
    private static IsPropertyChanged(change: any): change is IPropertyChange {
        return 'propertyName' in change;
    }
    private static IsCollectionChanged(change: any): change is ICollectionChange {
        return 'collectionName' in change;
    }
    private static IsCollectionItemAdded(change: any): change is ICollectionItemAdded {
        return 'item' in change;
    }
    private static IsCollectionItemRemoved(change: any): change is ICollectionItemRemoved {
        return 'removedItemId' in change;
    }
    private static IsCollectionItemReordered(change: any): change is ICollectionItemsReordered {
        return 'index1' in change;
    }

    private processPropertyChange(component: BaseViewModel, change: IPropertyChange) {
        if (change.id === AppComponent.id && change.propertyName == 'commandManager') {
            return; // you cannot set the reference of the command manager
        }
        //if (change.id == CommandManagerId && change.propertyName == 'commands') {
        //    return; // you also cannot set the reference of the commands
        //}

        (<any>component)[change.propertyName] = this.toInstanceOrCreate(change.value);
    }

    /** Adds the specified item to a collection, assuming no clientside command interferes. */
    private processItemAdded(collection: any[], change: ICollectionItemAdded) {
        collection.splice(change.index, 0, change.item);
        if (isComponent(change.item))
            this.toInstanceOrCreate(change.item);
    }

    private processItemRemoved(collection: any, change: ICollectionItemRemoved) {
        collection.splice(change.index, 1);
        // TODO: maybe removed from components?
        return;
    }
    //private createNewInstance(change: IViewModelInstantation) {
    //    this.components.set(change.value, { '__id': change.value });
    //}
}

export type admissibleTypes = (BaseViewModel & { isCollection: boolean }) | admissiblePrimitiveTypes;
type admissiblePrimitiveTypes = string | number | object;

interface IResponse {
    changes: (IPropertyChange | ICollectionItemAdded | ICollectionItemRemoved | ICollectionItemsReordered)[];
    rerequest: boolean;
}
interface IChange {
    /** The id of the view model containing the change. */
    id: number;
    instructionId: number;
}
interface IViewModelInstantation extends IPropertyChange {
    propertyName: '__id',
    value: number;
}
interface IPropertyChange extends IChange {
    propertyName: string;
    value: admissibleTypes;
}
interface ICollectionChange extends IChange {
    /** If this is undefined, then the component represented by the id is the collection alluded to; 
     * otherwise the view model with that id contains a property with that name that holds the collection. */
    collectionName: string | undefined;
}
interface ICollectionItemRemoved extends ICollectionChange {
    removedItemId: number | undefined;
    /** The index of the item that was removed. */
    index: number;
}
interface ICollectionItemAdded extends ICollectionChange {
    item: admissibleTypes;
    /** The index at which the item was inserted. */
    index: number;
}
interface ICollectionItemsReordered extends ICollectionChange {
    index1: number;
    index2: number;
}