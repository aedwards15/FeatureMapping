#import <UIKit/UIKit.h>
#import <MobileCoreServices/MobileCoreServices.h>

#if UNITY_VERSION <= 434
#import "iPhone_View.h"

#endif

char video_url_path[1024];

@interface NonRotatingUIImagePickerController : UIImagePickerController


@end

@implementation NonRotatingUIImagePickerController
- (NSUInteger)supportedInterfaceOrientations{
    return UIInterfaceOrientationMaskLandscape;
}
@end


//-----------------------------------------------------------------

@interface APLViewController : UIViewController <UINavigationControllerDelegate, UIImagePickerControllerDelegate>{

    UIImagePickerController *imagePickerController;
@public
    const char *callback_game_object_name ;
    const char *callback_function_name ;
}

@end


@implementation APLViewController


- (void)viewDidLoad
{
    [super viewDidLoad];

    [self showImagePickerForSourceType:UIImagePickerControllerSourceTypePhotoLibrary];

}


- (void)showImagePickerForSourceType:(UIImagePickerControllerSourceType)sourceType
{
    imagePickerController = [[UIImagePickerController alloc] init];
    imagePickerController.delegate = self;
    imagePickerController.allowsEditing = YES;
    imagePickerController.sourceType = sourceType;
    imagePickerController.modalPresentationStyle = UIModalPresentationCurrentContext;
    
    [self presentViewController:imagePickerController animated:YES completion:NULL];
    
    [self.view addSubview:imagePickerController.view];
}


#pragma mark - UIImagePickerControllerDelegate

// This method is called when an image has been chosen from the library or taken from the camera.
- (void)imagePickerController:(UIImagePickerController *)picker didFinishPickingMediaWithInfo:(NSDictionary *)info
{
    
    /*NSString *type = [info objectForKey:UIImagePickerControllerMediaType];
     //NSLog(@"type=%@",type);
     if ([type isEqualToString:(NSString *)kUTTypeVideo] ||
     [type isEqualToString:(NSString *)kUTTypeMovie])
     {// movie != video
     NSURL *urlvideo = [info objectForKey:UIImagePickerControllerMediaURL];
     NSLog(@"%@", urlvideo);
     NSString *urlString = [urlvideo absoluteString];
     const char* cp = [urlString UTF8String];
     strcpy(video_url_path, cp);
     }
     
     [self dismissViewControllerAnimated:YES completion:NULL];
     
     NSURL *url = [info objectForKey:UIImagePickerControllerReferenceURL];
     NSString *urlString = [url absoluteString];
     const char* cp = [urlString UTF8String];
     strcpy(video_url_path, cp);
     */
    
    NSURL *fileManagerURL = [[NSFileManager defaultManager] containerURLForSecurityApplicationGroupIdentifier:@"group.com.AngryElfStudios.PhotoData"];
    NSString *tmpPath = [NSString stringWithFormat:@"%@", fileManagerURL.path];
    NSString *finalPath = [NSString stringWithFormat:@"%@",[tmpPath stringByAppendingString:@"/myPhoto.jpg"]];
    
    
    NSData* imageData = UIImageJPEGRepresentation([info objectForKey:UIImagePickerControllerOriginalImage], 0.9);
    [imageData writeToFile:@"/private/var/mobile/Containers/Shared/AppGroup/92805BB8-0BDB-496D-A014-68589223B854/myPhoto.jpg" atomically:true];
    
    [self dismissViewControllerAnimated:YES completion:NULL];
    
    // UnitySendMessage("GameObject", "VideoPicked", video_url_path);
    UnitySendMessage(callback_game_object_name, callback_function_name, video_url_path);
}


- (void)imagePickerControllerDidCancel:(UIImagePickerController *)picker
{
    [self dismissViewControllerAnimated:YES completion:NULL];
}


@end





extern "C" {

    void OpenVideoPicker(const char *game_object_name, const char *function_name) {
        
        
        if ([UIImagePickerController isSourceTypeAvailable:UIImagePickerControllerSourceTypePhotoLibrary]) {
            // APLViewController
            UIViewController* parent = UnityGetGLViewController();
            APLViewController *uvc = [[APLViewController alloc] init];
            uvc->callback_game_object_name = strdup(game_object_name) ;
            uvc->callback_function_name = strdup(function_name) ;
            [parent presentViewController:uvc animated:YES completion:nil];
        }
    }
}
