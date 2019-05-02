import * as Fs from 'fs';
import * as Os from 'os';
import * as Path from 'path';
import { IpcService } from './ipc.service';
import { BaseService, Constants, MessageModel, MessageType, DirectoryModel, DirectoryModelSchema, Helpers, FileModel, ProviderModel, ProviderModelSchema } from '../../src-common';

export class FileSystemService extends BaseService {
    private readonly _ipc: IpcService;

    constructor(ipc: IpcService) {
        super();
        this._ipc = ipc;
        this._ipc.Receive.on(Constants.IPC_CHANNEL, (message: MessageModel) => this.OnMessage(message));
    }

    protected Dispose(): void {
        this._ipc.Receive.removeAllListeners(Constants.IPC_CHANNEL);
    }

    private async GetFileInfo(path: string): Promise<Fs.Stats> {
        return new Promise<Fs.Stats>((resolve, reject) => {
            Fs.lstat(path, (ex: NodeJS.ErrnoException, stats: Fs.Stats) => {
                if (ex)
                    reject(ex);
                else
                    resolve(stats);
            });
        });
    }

    private async GetFiles(path: string): Promise<string[]> {
        return new Promise<string[]>((resolve, reject) => {
            Fs.readdir(path, (ex: NodeJS.ErrnoException, fileNames: string[]) => {
                if (ex)
                    reject(ex);
                else
                    resolve(fileNames);
            })
        });
    }

    private async GetDirectory(path: string): Promise<DirectoryModel> {
        return new Promise<DirectoryModel>(async (resolve, reject) => {
            let directory = new DirectoryModel();
            directory.Name = path;
            directory.Files = [];
            directory.Directories = [];

            let fileNames: string[];
            try {
                fileNames = await this.GetFiles(path);
            } catch (ex) {
                reject(ex);
            }

            while (fileNames.length > 0) {
                let fileName = fileNames.pop();
                let fullName = Path.join(path, fileName);

                let info: Fs.Stats;
                try {
                    info = await this.GetFileInfo(fullName);
                } catch (ex) {
                    console.log(ex);
                    continue;
                }

                if (info.isDirectory()) {
                    let dir = new DirectoryModel();
                    dir.Name = fileName;
                    dir.Path = fullName;
                    directory.Directories.push(dir);
                } else if (info.isFile()) {
                    let file = new FileModel();
                    file.Name = fileName;
                    file.Path = fullName;
                    file.Size = info.size;
                    directory.Files.push(file);
                }
            }

            resolve(directory);
        });
    }

    private async GetProvider(): Promise<ProviderModel> {
        let provider = ProviderModel.New(`Computer (${Os.hostname()})`, 'fa fa-laptop');
        return new Promise<ProviderModel>(async (resolve, reject) => {
            Os.hostname();
            try {
                let directories = await this.GetDirectory(Os.homedir());
                provider.Directories = directories.Directories;
            } catch (ex) {
                console.log(ex);
            }

            if (provider)
                resolve(provider);
            else
                reject('Failed to get File System provider.');
        })
    }

    private OnMessage(message: MessageModel): void {
        switch (message.Type) {
            case MessageType.Directoties:
                this.GetDirectory(message.DataJson).then(directory => {
                    message.DataJson = Helpers.Serialize<DirectoryModel>(DirectoryModelSchema, directory);
                    this._ipc.Send(message);
                }).catch(ex =>
                    console.log(ex)
                );
                break;

            case MessageType.FileSystemProvider:
                this.GetProvider().then(provider => {
                    message.DataJson = Helpers.Serialize<ProviderModel>(ProviderModelSchema, provider);
                    this._ipc.Send(message);
                }).catch(ex =>
                    console.log(ex)
                );
                break;
        }
    }
}